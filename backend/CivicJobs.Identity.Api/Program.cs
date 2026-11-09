using CivicJobs.Infrastructure;
using CivicJobs.Infrastructure.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration, builder.Environment.ContentRootPath);
builder.AddCivicJobsWebApi("CivicJobs Identity API");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await app.Services.InitializeDatabaseAsync();
}

app.UseCivicJobsWebApi();

app.Run();
