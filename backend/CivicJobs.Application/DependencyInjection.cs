using CivicJobs.Application.InputModels;
using CivicJobs.Application.Services.Implementations;
using CivicJobs.Application.Services.Interfaces;
using CivicJobs.Application.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CivicJobs.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddSingleton<IValidator<CreateJobPostingInputModel>, CreateJobPostingInputModelValidator>();
        services.AddSingleton<IValidator<UpdateJobPostingInputModel>, UpdateJobPostingInputModelValidator>();
        services.AddSingleton<IValidator<RegisterInputModel>, RegisterInputModelValidator>();
        services.AddSingleton<IValidator<LoginInputModel>, LoginInputModelValidator>();

        services.AddScoped<IJobPostingService, JobPostingService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
