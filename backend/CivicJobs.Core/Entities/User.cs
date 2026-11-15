using CivicJobs.Core.Enums;
using CivicJobs.Core.Exceptions;

namespace CivicJobs.Core.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public Guid? AgencyId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private User()
    {
    }

    /// <summary>A candidate from public registration: no agency and no department (BR-13).</summary>
    public static User CreateCandidate(string email, string fullName, string passwordHash, DateTimeOffset createdAt) =>
        Create(email, fullName, passwordHash, UserRole.Candidate, agencyId: null, departmentId: null, createdAt);

    /// <summary>An admin, recruiter or hiring manager of an agency, created by an admin (BR-13).</summary>
    public static User CreateStaff(
        string email,
        string fullName,
        string passwordHash,
        UserRole role,
        Guid agencyId,
        Guid? departmentId,
        DateTimeOffset createdAt)
    {
        if (agencyId == Guid.Empty)
        {
            throw new DomainValidationException("AgencyId is required for a staff account.");
        }

        EnsureStaffPlacement(role, departmentId);

        return Create(email, fullName, passwordHash, role, agencyId, departmentId, createdAt);
    }

    public void ChangeRole(UserRole role, Guid? departmentId)
    {
        if (Role == UserRole.Candidate)
        {
            throw new DomainValidationException("A candidate account can't get a staff role.");
        }

        EnsureStaffPlacement(role, departmentId);

        Role = role;
        DepartmentId = departmentId;
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    private static User Create(
        string email,
        string fullName,
        string passwordHash,
        UserRole role,
        Guid? agencyId,
        Guid? departmentId,
        DateTimeOffset createdAt)
    {
        Guard.Required(email, nameof(Email));
        Guard.Required(fullName, nameof(FullName));
        Guard.Required(passwordHash, nameof(PasswordHash));

        return new User
        {
            Id = Guid.NewGuid(),
            Email = NormalizeEmail(email),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
            Role = role,
            AgencyId = agencyId,
            DepartmentId = departmentId,
            CreatedAt = createdAt
        };
    }

    private static void EnsureStaffPlacement(UserRole role, Guid? departmentId)
    {
        switch (role)
        {
            case UserRole.Admin when departmentId is not null:
                throw new DomainValidationException("An admin belongs to the agency, not to a department.");
            case UserRole.Recruiter or UserRole.HiringManager when departmentId is null || departmentId == Guid.Empty:
                throw new DomainValidationException("Choose a department for a recruiter or hiring manager.");
            case UserRole.Admin or UserRole.Recruiter or UserRole.HiringManager:
                return;
            default:
                throw new DomainValidationException("Choose a staff role: admin, recruiter or hiring manager.");
        }
    }
}
