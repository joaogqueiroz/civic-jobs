using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services;
using CivicJobs.Core.Enums;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace CivicJobs.UnitTests.Application.Services;

public class JobPostingServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 8, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeTimeProvider _timeProvider = new(Now);

    [Fact]
    [Trait("Requirement", "FR-04")]
    public async Task CreateAsync_WithValidInput_ReturnsDraftPostingWithInputData()
    {
        var input = ValidInput();

        var result = await CreateService().CreateAsync(Guid.NewGuid(), input, CancellationToken.None);

        result.ShouldSatisfyAllConditions(
            () => result.Id.ShouldNotBe(Guid.Empty),
            () => result.Title.ShouldBe(input.Title),
            () => result.Department.ShouldBe(input.Department),
            () => result.Description.ShouldBe(input.Description),
            () => result.Location.ShouldBe(input.Location),
            () => result.EmploymentType.ShouldBe(input.EmploymentType),
            () => result.SalaryMin.ShouldBe(input.SalaryMin),
            () => result.SalaryMax.ShouldBe(input.SalaryMax),
            () => result.ClosingDate.ShouldBe(input.ClosingDate),
            () => result.Status.ShouldBe(JobPostingStatus.Draft),
            () => result.CreatedAt.ShouldBe(Now),
            () => result.UpdatedAt.ShouldBe(Now));
    }

    private JobPostingService CreateService() => new(_timeProvider);

    private static CreateJobPostingInputModel ValidInput() => new()
    {
        Title = "Park Ranger",
        Department = "Parks & Recreation",
        Description = "Patrol city parks and assist visitors.",
        Location = "Central Park District",
        EmploymentType = EmploymentType.PartTime,
        SalaryMin = 50_000m,
        SalaryMax = 60_000m,
        ClosingDate = new DateOnly(2026, 12, 31)
    };
}
