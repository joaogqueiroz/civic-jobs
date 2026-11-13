using CivicJobs.Application;
using CivicJobs.Infrastructure;
using CivicJobs.Infrastructure.Web;
using CivicJobs.Jobs.Api.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.AddCivicJobsWebApi("CivicJobs Jobs API");
builder.Services.AddHostedService<JobPostingExpirationWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeDatabaseAsync();
}

app.UseCivicJobsWebApi();

app.Run();
