using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Exceptions;
using Shouldly;

namespace CivicJobs.UnitTests.Core.Entities;

public class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid AgencyId = Guid.NewGuid();
    private static readonly Guid DepartmentId = Guid.NewGuid();

    [Fact]
    [Trait("Requirement", "FR-01")]
    public void CreateCandidate_WithValidData_SetsFields()
    {
        var user = User.CreateCandidate("recruiter@cityofexample.gov", "Jane Doe", "hashed-password", Now);

        user.ShouldSatisfyAllConditions(
            () => user.Id.ShouldNotBe(Guid.Empty),
            () => user.Email.ShouldBe("recruiter@cityofexample.gov"),
            () => user.FullName.ShouldBe("Jane Doe"),
            () => user.PasswordHash.ShouldBe("hashed-password"),
            () => user.CreatedAt.ShouldBe(Now));
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public void CreateCandidate_WithMixedCaseEmail_StoresEmailInLowercase()
    {
        var user = User.CreateCandidate("Recruiter@CityOfExample.GOV", "Jane Doe", "hashed-password", Now);

        user.Email.ShouldBe("recruiter@cityofexample.gov");
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public void CreateCandidate_WithSurroundingSpacesInEmail_StoresTrimmedEmail()
    {
        var user = User.CreateCandidate("  recruiter@cityofexample.gov  ", "Jane Doe", "hashed-password", Now);

        user.Email.ShouldBe("recruiter@cityofexample.gov");
    }

    [Fact]
    [Trait("Requirement", "FR-01")]
    public void CreateCandidate_WithSurroundingSpacesInFullName_StoresTrimmedFullName()
    {
        var user = User.CreateCandidate("recruiter@cityofexample.gov", "  Jane Doe  ", "hashed-password", Now);

        user.FullName.ShouldBe("Jane Doe");
    }

    [Fact]
    [Trait("Rule", "BR-01")]
    public void NormalizeEmail_WithMixedCaseAndSpaces_ReturnsTrimmedLowercase()
    {
        User.NormalizeEmail(" Recruiter@CityOfExample.GOV ").ShouldBe("recruiter@cityofexample.gov");
    }

    [Theory]
    [Trait("Requirement", "FR-01")]
    [InlineData("email")]
    [InlineData("fullName")]
    [InlineData("passwordHash")]
    public void CreateCandidate_WhenRequiredFieldIsMissing_ThrowsDomainValidationException(string field)
    {
        var create = () => User.CreateCandidate(
            email: field == "email" ? " " : "recruiter@cityofexample.gov",
            fullName: field == "fullName" ? " " : "Jane Doe",
            passwordHash: field == "passwordHash" ? " " : "hashed-password",
            createdAt: Now);

        create.ShouldThrow<DomainValidationException>();
    }

    #region Roles (BR-13)

    [Fact]
    [Trait("Requirement", "FR-01")]
    [Trait("Rule", "BR-13")]
    public void CreateCandidate_SetsCandidateRoleWithoutAgencyOrDepartment()
    {
        var user = User.CreateCandidate("candidate@example.com", "Alex Kim", "hashed-password", Now);

        user.ShouldSatisfyAllConditions(
            () => user.Role.ShouldBe(UserRole.Candidate),
            () => user.AgencyId.ShouldBeNull(),
            () => user.DepartmentId.ShouldBeNull());
    }

    [Theory]
    [Trait("Requirement", "FR-13")]
    [Trait("Rule", "BR-13")]
    [InlineData(UserRole.Recruiter)]
    [InlineData(UserRole.HiringManager)]
    public void CreateStaff_ForDepartmentRole_SetsRoleAgencyAndDepartment(UserRole role)
    {
        var user = Staff(role, DepartmentId);

        user.ShouldSatisfyAllConditions(
            () => user.Role.ShouldBe(role),
            () => user.AgencyId.ShouldBe(AgencyId),
            () => user.DepartmentId.ShouldBe(DepartmentId),
            () => user.Email.ShouldBe("staff@cityofexample.gov"));
    }

    [Fact]
    [Trait("Requirement", "FR-13")]
    [Trait("Rule", "BR-13")]
    public void CreateStaff_ForAdmin_SetsAgencyWithoutDepartment()
    {
        var user = Staff(UserRole.Admin, departmentId: null);

        user.Role.ShouldBe(UserRole.Admin);
        user.AgencyId.ShouldBe(AgencyId);
        user.DepartmentId.ShouldBeNull();
    }

    [Theory]
    [Trait("Rule", "BR-13")]
    [InlineData(UserRole.Recruiter)]
    [InlineData(UserRole.HiringManager)]
    public void CreateStaff_ForDepartmentRoleWithoutDepartment_ThrowsDomainValidationException(UserRole role)
    {
        var create = () => Staff(role, departmentId: null);

        create.ShouldThrow<DomainValidationException>()
            .Message.ShouldBe("Choose a department for a recruiter or hiring manager.");
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public void CreateStaff_ForAdminWithDepartment_ThrowsDomainValidationException()
    {
        var create = () => Staff(UserRole.Admin, DepartmentId);

        create.ShouldThrow<DomainValidationException>()
            .Message.ShouldBe("An admin belongs to the agency, not to a department.");
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public void CreateStaff_ForCandidateRole_ThrowsDomainValidationException()
    {
        var create = () => Staff(UserRole.Candidate, departmentId: null);

        create.ShouldThrow<DomainValidationException>();
    }

    [Theory]
    [Trait("Rule", "BR-13")]
    [InlineData(0)]
    [InlineData(99)]
    public void CreateStaff_WithUndefinedRole_ThrowsDomainValidationException(int role)
    {
        var create = () => Staff((UserRole)role, DepartmentId);

        create.ShouldThrow<DomainValidationException>();
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public void CreateStaff_WithoutAgency_ThrowsDomainValidationException()
    {
        var create = () => User.CreateStaff("staff@cityofexample.gov", "Sam Patel", "hashed-password",
            UserRole.Recruiter, Guid.Empty, DepartmentId, Now);

        create.ShouldThrow<DomainValidationException>();
    }

    [Fact]
    [Trait("Requirement", "FR-14")]
    public void ChangeRole_FromRecruiterToHiringManagerOfAnotherDepartment_ChangesRoleAndDepartment()
    {
        var user = Staff(UserRole.Recruiter, DepartmentId);
        var otherDepartment = Guid.NewGuid();

        user.ChangeRole(UserRole.HiringManager, otherDepartment);

        user.Role.ShouldBe(UserRole.HiringManager);
        user.DepartmentId.ShouldBe(otherDepartment);
        user.AgencyId.ShouldBe(AgencyId);
    }

    [Fact]
    [Trait("Requirement", "FR-14")]
    public void ChangeRole_FromRecruiterToAdmin_ClearsDepartment()
    {
        var user = Staff(UserRole.Recruiter, DepartmentId);

        user.ChangeRole(UserRole.Admin, departmentId: null);

        user.Role.ShouldBe(UserRole.Admin);
        user.DepartmentId.ShouldBeNull();
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public void ChangeRole_ToRecruiterWithoutDepartment_ThrowsDomainValidationException()
    {
        var user = Staff(UserRole.Admin, departmentId: null);

        var change = () => user.ChangeRole(UserRole.Recruiter, departmentId: null);

        change.ShouldThrow<DomainValidationException>();
        user.Role.ShouldBe(UserRole.Admin);
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public void ChangeRole_ToCandidate_ThrowsDomainValidationException()
    {
        var user = Staff(UserRole.Recruiter, DepartmentId);

        var change = () => user.ChangeRole(UserRole.Candidate, departmentId: null);

        change.ShouldThrow<DomainValidationException>();
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public void ChangeRole_OfCandidate_ThrowsDomainValidationException()
    {
        var user = User.CreateCandidate("candidate@example.com", "Alex Kim", "hashed-password", Now);

        var change = () => user.ChangeRole(UserRole.Recruiter, DepartmentId);

        change.ShouldThrow<DomainValidationException>()
            .Message.ShouldBe("A candidate account can't get a staff role.");
    }

    private static User Staff(UserRole role, Guid? departmentId) =>
        User.CreateStaff("Staff@CityOfExample.gov", "Sam Patel", "hashed-password", role, AgencyId, departmentId, Now);

    #endregion
}
