namespace CivicJobs.Core.Entities;

public class Agency
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }

    private Agency()
    {
    }

    public static Agency Create(string name, DateTimeOffset createdAt)
    {
        Guard.Required(name, nameof(Name));

        return new Agency
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CreatedAt = createdAt
        };
    }
}
