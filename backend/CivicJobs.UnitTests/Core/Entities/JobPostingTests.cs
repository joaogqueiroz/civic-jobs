using CivicJobs.Core.Entities;
using CivicJobs.Core.Enums;
using CivicJobs.Core.Exceptions;
using Shouldly;

namespace CivicJobs.UnitTests.Core.Entities;

public class JobPostingTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Rule", "BR-04")]
    public void Create_WhenSalaryMaxIsLessThanSalaryMin_ThrowsDomainValidationException()
    {
        var create = () => JobPosting.Create(
            createdByUserId: Guid.NewGuid(),
            title: "Park Ranger",
            department: "Parks & Recreation",
            description: "Patrol city parks and assist visitors.",
            location: "Central Park District",
            employmentType: EmploymentType.FullTime,
            salaryMin: 50_000m,
            salaryMax: 49_999.99m,
            closingDate: new DateOnly(2026, 12, 31),
            createdAt: Now);

        create.ShouldThrow<DomainValidationException>();
    }
}
