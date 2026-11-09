using System.Text.Json.Serialization;
using TaskManager.Api.Auth;
using TaskManager.Api.Development;
using TaskManager.Api.ErrorHandling;
using TaskManager.Api.Swagger;
using TaskManager.Application;
using TaskManager.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

builder.Services
    .AddApplication()
    .AddInfrastructure()
    .AddJwtAuthentication();

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

if (isDevelopment)
{
    builder.Services.AddSwaggerWithJwt();
    builder.Services.AddDevTokenIssuer();
}

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();
// Gives empty-bodied errors (401, 403, 404 for unknown routes, …) a ProblemDetails body.
app.UseStatusCodePages();

if (isDevelopment)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (isDevelopment)
{
    app.MapDevTokenEndpoint();
}

await app.RunAsync();

// Exposes Program to WebApplicationFactory<Program> in the endpoint tests.
public partial class Program;
