using CivicJobs.Application.Exceptions;
using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.ViewModels;
using CivicJobs.Core.Entities;
using CivicJobs.Core.Repositories;
using FluentValidation;

namespace CivicJobs.Application.Services.Implementations;

public sealed class DepartmentService(
    IValidator<CreateDepartmentInputModel> validator,
    IUserRepository users,
    IDepartmentRepository departments,
    TimeProvider timeProvider) : IDepartmentService
{
    public async Task<IReadOnlyList<DepartmentViewModel>> ListAsync(Guid adminId, CancellationToken cancellationToken)
    {
        var agencyId = await users.GetAgencyIdOfAsync(adminId, cancellationToken);
        var list = await departments.ListByAgencyAsync(agencyId, cancellationToken);

        return list.Select(DepartmentViewModel.FromEntity).ToList();
    }

    public async Task<DepartmentViewModel> CreateAsync(
        Guid adminId,
        CreateDepartmentInputModel input,
        CancellationToken cancellationToken)
    {
        var agencyId = await users.GetAgencyIdOfAsync(adminId, cancellationToken);

        await validator.ValidateAndThrowAsync(input, cancellationToken);

        var department = Department.Create(agencyId, input.Name, timeProvider.GetUtcNow());

        if (await departments.ExistsAsync(agencyId, department.Name, cancellationToken))
        {
            throw new ConflictException("Your agency already has a department with this name.");
        }

        await departments.AddAsync(department, cancellationToken);

        return DepartmentViewModel.FromEntity(department);
    }
}
