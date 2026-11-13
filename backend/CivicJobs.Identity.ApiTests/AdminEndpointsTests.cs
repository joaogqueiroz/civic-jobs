using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Enums;
using Shouldly;

namespace CivicJobs.Identity.ApiTests;

public sealed class AdminEndpointsTests(IdentityApiFactory factory) : IClassFixture<IdentityApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    [Trait("Requirement", "FR-03")]
    public async Task GetMe_AsHiringManager_ReturnsRoleAgencyAndDepartment()
    {
        var department = await factory.CreateDepartmentAsync("Public Works");
        var client = await factory.CreateClientForAsync(UserRole.HiringManager, department);

        var me = await client.GetFromJsonAsync<UserViewModel>("/api/users/me", Json);

        me.ShouldNotBeNull();
        me.ShouldSatisfyAllConditions(
            () => me.Role.ShouldBe(UserRole.HiringManager),
            () => me.AgencyId.ShouldBe(department.AgencyId),
            () => me.DepartmentName.ShouldBe("Public Works"));
    }

    [Theory]
    [Trait("Rule", "BR-12")]
    [InlineData(UserRole.Recruiter)]
    [InlineData(UserRole.HiringManager)]
    [InlineData(UserRole.Candidate)]
    public async Task ListDepartments_WithRoleOtherThanAdmin_Returns403(UserRole role)
    {
        var client = await factory.CreateClientForAsync(role);

        var response = await client.GetAsync("/api/departments");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    [Trait("Requirement", "FR-12")]
    public async Task CreateDepartment_AsAdmin_Returns201AndListsItWithTheAgencysDepartments()
    {
        var parks = await factory.CreateDepartmentAsync();
        var admin = await factory.CreateClientForAsync(UserRole.Admin, parks);

        var response = await admin.PostAsJsonAsync("/api/departments", new { name = "Libraries" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var departments = await admin.GetFromJsonAsync<List<DepartmentViewModel>>("/api/departments", Json);
        departments!.Select(d => d.Name).ShouldBe(["Libraries", "Parks & Recreation"]);
    }

    [Fact]
    [Trait("Rule", "BR-14")]
    public async Task CreateDepartment_WithNameInUseInTheAgency_Returns409()
    {
        var parks = await factory.CreateDepartmentAsync();
        var admin = await factory.CreateClientForAsync(UserRole.Admin, parks);

        var response = await admin.PostAsJsonAsync("/api/departments", new { name = "PARKS & RECREATION" });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    [Trait("Requirement", "FR-13")]
    public async Task CreateStaff_AsAdmin_Returns201AndTheNewUserCanLogIn()
    {
        var parks = await factory.CreateDepartmentAsync();
        var admin = await factory.CreateClientForAsync(UserRole.Admin, parks);
        var email = $"new-{Guid.NewGuid():N}@cityofexample.gov";

        var response = await admin.PostAsJsonAsync("/api/users", new
        {
            fullName = "Sam Patel",
            email,
            password = "Demo@12345",
            role = "Recruiter",
            departmentId = parks.Id
        });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var staff = await response.Content.ReadFromJsonAsync<StaffViewModel>(Json);
        staff!.DepartmentName.ShouldBe("Parks & Recreation");
        var login = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new { email, password = "Demo@12345" });
        login.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    [Trait("Rule", "BR-15")]
    public async Task CreateStaff_InDepartmentOfAnotherAgency_Returns404()
    {
        var admin = await factory.CreateClientForAsync(UserRole.Admin);
        var otherAgencyDepartment = await factory.CreateDepartmentAsync();

        var response = await admin.PostAsJsonAsync("/api/users", new
        {
            fullName = "Sam Patel",
            email = $"new-{Guid.NewGuid():N}@cityofexample.gov",
            password = "Demo@12345",
            role = "Recruiter",
            departmentId = otherAgencyDepartment.Id
        });

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    [Trait("Requirement", "FR-14")]
    public async Task ChangeRole_OfAStaffMemberOfTheAgency_Returns200WithTheNewRole()
    {
        var parks = await factory.CreateDepartmentAsync();
        var admin = await factory.CreateClientForAsync(UserRole.Admin, parks);
        await factory.CreateClientForAsync(UserRole.Recruiter, parks);
        var recruiter = (await ListStaffAsync(admin)).Single(s => s.Role == UserRole.Recruiter);

        var response = await admin.PutAsJsonAsync($"/api/users/{recruiter.Id}/role",
            new { role = "HiringManager", departmentId = parks.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<StaffViewModel>(Json))!.Role.ShouldBe(UserRole.HiringManager);
    }

    [Fact]
    [Trait("Rule", "BR-22")]
    public async Task ChangeRole_OfTheAdminThemself_Returns409()
    {
        var parks = await factory.CreateDepartmentAsync();
        var admin = await factory.CreateClientForAsync(UserRole.Admin, parks);
        var self = (await ListStaffAsync(admin)).Single(s => s.Role == UserRole.Admin);

        var response = await admin.PutAsJsonAsync($"/api/users/{self.Id}/role",
            new { role = "Recruiter", departmentId = parks.Id });

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    private static async Task<List<StaffViewModel>> ListStaffAsync(HttpClient admin) =>
        (await admin.GetFromJsonAsync<List<StaffViewModel>>("/api/users", Json))!;
}
