using CivicJobs.Core.Exceptions;

namespace CivicJobs.Core.Entities;

public class Department
{
    public Guid Id { get; private set; }
    public Guid AgencyId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private Department()
    {
    }

    public static Department Create(Guid agencyId, string name, DateTimeOffset createdAt)
    {
        if (agencyId == Guid.Empty)
        {
            throw new DomainValidationException("AgencyId is required.");
        }

        Guard.Required(name, nameof(Name));

        return new Department
        {
            Id = Guid.NewGuid(),
            AgencyId = agencyId,
            Name = name.Trim(),
            CreatedAt = createdAt
        };
    }
}
