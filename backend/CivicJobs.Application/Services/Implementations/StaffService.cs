using CivicJobs.Application.Abstractions;
using CivicJobs.Application.Exceptions;
using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using FluentValidation;

namespace CivicJobs.Application.Services.Implementations;

public sealed class StaffService(
    IValidator<CreateStaffInputModel> createValidator,
    IValidator<ChangeRoleInputModel> changeRoleValidator,
    IUserRepository users,
    IDepartmentRepository departments,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider) : IStaffService
{
    public async Task<IReadOnlyList<StaffViewModel>> ListAsync(Guid adminId, CancellationToken cancellationToken)
    {
        var agencyId = await users.GetAgencyIdOfAsync(adminId, cancellationToken);
        var staff = await users.ListStaffByAgencyAsync(agencyId, cancellationToken);
        var departmentNames = (await departments.ListByAgencyAsync(agencyId, cancellationToken))
            .ToDictionary(d => d.Id, d => d.Name);

        return staff
            .Select(user => StaffViewModel.FromEntity(
                user,
                user.DepartmentId is { } id ? departmentNames.GetValueOrDefault(id) : null))
            .ToList();
    }

    public async Task<StaffViewModel> CreateAsync(Guid adminId, CreateStaffInputModel input, CancellationToken cancellationToken)
    {
        var agencyId = await users.GetAgencyIdOfAsync(adminId, cancellationToken);

        await createValidator.ValidateAndThrowAsync(input, cancellationToken);

        if (await users.GetByEmailAsync(User.NormalizeEmail(input.Email), cancellationToken) is not null)
        {
            throw new ConflictException("An account with this email already exists. Use a different email.");
        }

        var department = await GetAgencyDepartmentAsync(agencyId, input.DepartmentId, cancellationToken);

        var user = User.CreateStaff(
            input.Email,
            input.FullName,
            passwordHasher.Hash(input.Password),
            input.Role,
            agencyId,
            department?.Id,
            timeProvider.GetUtcNow());

        await users.AddAsync(user, cancellationToken);

        return StaffViewModel.FromEntity(user, department?.Name);
    }

    public async Task<StaffViewModel> ChangeRoleAsync(
        Guid adminId,
        Guid userId,
        ChangeRoleInputModel input,
        CancellationToken cancellationToken)
    {
        // Order of checks: 404 (not staff of this agency), 409 (own role), 400 (invalid input), 404 (department).
        var agencyId = await users.GetAgencyIdOfAsync(adminId, cancellationToken);
        var user = await users.GetByIdAsync(userId, cancellationToken);

        if (user is null || user.AgencyId != agencyId)
        {
            throw new NotFoundException("We couldn't find that staff member.");
        }

        // BR-22: the admin making changes stays an admin, so the agency is never left without one.
        if (userId == adminId)
        {
            throw new ConflictException("You can't change your own role. Ask another admin of your agency.");
        }

        await changeRoleValidator.ValidateAndThrowAsync(input, cancellationToken);

        var department = await GetAgencyDepartmentAsync(agencyId, input.DepartmentId, cancellationToken);

        user.ChangeRole(input.Role, department?.Id);
        await users.UpdateAsync(user, cancellationToken);

        return StaffViewModel.FromEntity(user, department?.Name);
    }

    private async Task<Department?> GetAgencyDepartmentAsync(Guid agencyId, Guid? departmentId, CancellationToken cancellationToken)
    {
        if (departmentId is null)
        {
            return null;
        }

        var department = await departments.GetByIdAsync(departmentId.Value, cancellationToken);

        // A department of another agency is reported as not found (BR-15).
        return department is not null && department.AgencyId == agencyId
            ? department
            : throw new NotFoundException("We couldn't find that department.");
    }
}
