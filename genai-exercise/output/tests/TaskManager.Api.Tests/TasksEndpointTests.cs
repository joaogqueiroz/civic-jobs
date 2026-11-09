using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using TaskManager.Api.Tests.Infrastructure;
using TaskManager.Application.Tasks.Dtos;
using TaskManager.Core.Enums;

namespace TaskManager.Api.Tests;

public sealed class TasksEndpointTests(TaskApiFactory factory) : IClassFixture<TaskApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static object NewTaskBody(string title = "Endpoint task") =>
        new { title, description = "from tests", dueDate = TaskApiFactory.Today.AddDays(3) };

    private static async Task<TaskResponse> CreateTaskAsync(HttpClient client, string title = "Endpoint task")
    {
        var response = await client.PostAsJsonAsync("/api/tasks", NewTaskBody(title), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
    }

    // ---- 401 ----------------------------------------------------------------

    [Theory]
    [InlineData("GET", "/api/tasks")]
    [InlineData("GET", "/api/tasks/7d0f1d55-9a8e-4e1a-9a9e-2d8b8a1f0c11")]
    [InlineData("POST", "/api/tasks")]
    [InlineData("PUT", "/api/tasks/7d0f1d55-9a8e-4e1a-9a9e-2d8b8a1f0c11")]
    [InlineData("DELETE", "/api/tasks/7d0f1d55-9a8e-4e1a-9a9e-2d8b8a1f0c11")]
    public async Task Requests_without_token_return_401(string method, string url)
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url)
        {
            Content = method is "POST" or "PUT" ? JsonContent.Create(NewTaskBody(), options: Json) : null
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Request_with_token_signed_by_another_key_returns_401()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", TestTokens.Create(Guid.NewGuid(), "a-completely-different-key-also-32-bytes-long!"));

        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- 404 for another user's task ----------------------------------------

    [Fact]
    public async Task Another_users_task_returns_404_for_get_put_and_delete()
    {
        var owner = factory.CreateClientFor(Guid.NewGuid());
        var intruder = factory.CreateClientFor(Guid.NewGuid());
        var task = await CreateTaskAsync(owner);
        var url = $"/api/tasks/{task.Id}";

        var get = await intruder.GetAsync(url);
        var put = await intruder.PutAsJsonAsync(url,
            new { title = "Hijacked", status = "Done", dueDate = task.DueDate }, Json);
        var delete = await intruder.DeleteAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, put.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        Assert.Equal("application/problem+json", get.Content.Headers.ContentType?.MediaType);

        // The owner's task is untouched and the intruder's list doesn't include it.
        var stillThere = await owner.GetFromJsonAsync<TaskResponse>(url, Json);
        Assert.Equal(task.Title, stillThere!.Title);
        var intruderList = await intruder.GetFromJsonAsync<List<TaskResponse>>("/api/tasks", Json);
        Assert.DoesNotContain(intruderList!, t => t.Id == task.Id);
    }

    // ---- 400 ------------------------------------------------------------------

    [Fact]
    public async Task Create_with_whitespace_title_returns_400_validation_problem()
    {
        var client = factory.CreateClientFor(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/tasks", NewTaskBody(title: "   "), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.Equal(400, problem!.Status);
        Assert.Contains("Title", problem.Errors.Keys);
    }

    [Fact]
    public async Task Moving_done_task_back_to_todo_returns_400()
    {
        var client = factory.CreateClientFor(Guid.NewGuid());
        var task = await CreateTaskAsync(client);
        var url = $"/api/tasks/{task.Id}";
        (await client.PutAsJsonAsync(url, new { title = task.Title, status = "Done", dueDate = task.DueDate }, Json))
            .EnsureSuccessStatusCode();

        var response = await client.PutAsJsonAsync(url,
            new { title = task.Title, status = "Todo", dueDate = task.DueDate }, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.Contains("Status", problem!.Errors.Keys);
    }

    // ---- Happy path -----------------------------------------------------------

    [Fact]
    public async Task Full_crud_flow_for_the_owner()
    {
        var client = factory.CreateClientFor(Guid.NewGuid());

        var post = await client.PostAsJsonAsync("/api/tasks", NewTaskBody("Lifecycle"), Json);
        Assert.Equal(HttpStatusCode.Created, post.StatusCode);
        var created = (await post.Content.ReadFromJsonAsync<TaskResponse>(Json))!;
        Assert.NotNull(post.Headers.Location);
        Assert.EndsWith($"/api/tasks/{created.Id}", post.Headers.Location!.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(TaskItemStatus.Todo, created.Status);

        var fetched = await client.GetFromJsonAsync<TaskResponse>(post.Headers.Location, Json);
        Assert.Equal(created.Id, fetched!.Id);

        var put = await client.PutAsJsonAsync($"/api/tasks/{created.Id}",
            new { title = "Lifecycle v2", status = "Doing", dueDate = created.DueDate }, Json);
        Assert.Equal(HttpStatusCode.OK, put.StatusCode);

        var doing = await client.GetFromJsonAsync<List<TaskResponse>>("/api/tasks?status=Doing", Json);
        Assert.Equal("Lifecycle v2", Assert.Single(doing!).Title);
        var todo = await client.GetFromJsonAsync<List<TaskResponse>>("/api/tasks?status=Todo", Json);
        Assert.Empty(todo!);

        var delete = await client.DeleteAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/tasks/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Dev_token_endpoint_does_not_exist_outside_development()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/dev/token", new { userId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
