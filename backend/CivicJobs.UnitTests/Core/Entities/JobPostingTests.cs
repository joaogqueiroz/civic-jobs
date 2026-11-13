using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Exceptions;
using Shouldly;

namespace CivicJobs.UnitTests.Core.Entities;

public class JobPostingTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    [Fact]
    [Trait("Requirement", "FR-04")]
    public void Create_WithValidData_SetsStatusToDraft()
    {
        var posting = CreatePosting();

        posting.Status.ShouldBe(JobPostingStatus.Draft);
    }

    [Fact]
    [Trait("Rule", "BR-04")]
    public void Create_WhenSalaryMaxIsLessThanSalaryMin_ThrowsDomainValidationException()
    {
        var create = () => CreatePosting(salaryMin: 50_000m, salaryMax: 49_999.99m);

        create.ShouldThrow<DomainValidationException>();
    }

    [Fact]
    [Trait("Rule", "BR-04")]
    public void Create_WhenSalaryMaxEqualsSalaryMin_CreatesPosting()
    {
        var posting = CreatePosting(salaryMin: 50_000m, salaryMax: 50_000m);

        posting.SalaryMin.ShouldBe(50_000m);
        posting.SalaryMax.ShouldBe(50_000m);
    }

    [Theory]
    [Trait("Rule", "BR-04")]
    [InlineData(0)]
    [InlineData(-0.01)]
    public void Create_WhenSalaryMinIsNotPositive_ThrowsDomainValidationException(double salaryMin)
    {
        var create = () => CreatePosting(salaryMin: (decimal)salaryMin, salaryMax: 60_000m);

        create.ShouldThrow<DomainValidationException>();
    }

    [Fact]
    [Trait("Rule", "BR-08")]
    public void Create_WhenCreatedByUserIdIsEmpty_ThrowsDomainValidationException()
    {
        var create = () => CreatePosting(createdByUserId: Guid.Empty);

        create.ShouldThrow<DomainValidationException>();
    }

    [Fact]
    [Trait("Requirement", "FR-04")]
    public void Create_WithDepartment_SetsDepartmentIdAndName()
    {
        var departmentId = Guid.NewGuid();

        var posting = CreatePosting(departmentId: departmentId, department: "Public Works");

        posting.DepartmentId.ShouldBe(departmentId);
        posting.Department.ShouldBe("Public Works");
    }

    [Fact]
    [Trait("Rule", "BR-13")]
    public void Create_WhenDepartmentIdIsEmpty_ThrowsDomainValidationException()
    {
        var create = () => CreatePosting(departmentId: Guid.Empty);

        create.ShouldThrow<DomainValidationException>();
    }

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenTitleIsMissing_ThrowsDomainValidationException(string? title)
    {
        var create = () => CreatePosting(title: title!);

        create.ShouldThrow<DomainValidationException>();
    }

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenDepartmentIsMissing_ThrowsDomainValidationException(string? department)
    {
        var create = () => CreatePosting(department: department!);

        create.ShouldThrow<DomainValidationException>();
    }

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenDescriptionIsMissing_ThrowsDomainValidationException(string? description)
    {
        var create = () => CreatePosting(description: description!);

        create.ShouldThrow<DomainValidationException>();
    }

    [Theory]
    [Trait("Rule", "BR-03")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WhenLocationIsMissing_ThrowsDomainValidationException(string? location)
    {
        var create = () => CreatePosting(location: location!);

        create.ShouldThrow<DomainValidationException>();
    }

    [Fact]
    [Trait("Rule", "BR-09")]
    public void Create_WhenClosingDateIsInThePast_ThrowsDomainValidationException()
    {
        var create = () => CreatePosting(closingDate: Today.AddDays(-1));

        create.ShouldThrow<DomainValidationException>();
    }

    [Fact]
    [Trait("Rule", "BR-09")]
    public void Create_WhenClosingDateIsToday_CreatesPosting()
    {
        var posting = CreatePosting(closingDate: Today);

        posting.ClosingDate.ShouldBe(Today);
    }

    [Theory]
    [Trait("Rule", "BR-10")]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(99)]
    public void Create_WhenEmploymentTypeIsNotDefined_ThrowsDomainValidationException(int employmentType)
    {
        var create = () => CreatePosting(employmentType: (EmploymentType)employmentType);

        create.ShouldThrow<DomainValidationException>();
    }

    private static JobPosting CreatePosting(
        Guid? createdByUserId = null,
        Guid? departmentId = null,
        string title = "Park Ranger",
        string department = "Parks & Recreation",
        string description = "Patrol city parks and assist visitors.",
        string location = "Central Park District",
        EmploymentType employmentType = EmploymentType.FullTime,
        decimal salaryMin = 50_000m,
        decimal salaryMax = 60_000m,
        DateOnly? closingDate = null) =>
        JobPosting.Create(
            createdByUserId: createdByUserId ?? Guid.NewGuid(),
            title: title,
            departmentId: departmentId ?? Guid.NewGuid(),
            department: department,
            description: description,
            location: location,
            employmentType: employmentType,
            salaryMin: salaryMin,
            salaryMax: salaryMax,
            closingDate: closingDate ?? new DateOnly(2026, 12, 31),
            createdAt: Now);
}
