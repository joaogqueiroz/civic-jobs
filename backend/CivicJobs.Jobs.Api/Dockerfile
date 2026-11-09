# Build context: backend/ (see docker-compose.yml)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from the project files only, so this layer is cached until a dependency changes.
COPY CivicJobs.Core/CivicJobs.Core.csproj CivicJobs.Core/
COPY CivicJobs.Application/CivicJobs.Application.csproj CivicJobs.Application/
COPY CivicJobs.Infrastructure/CivicJobs.Infrastructure.csproj CivicJobs.Infrastructure/
COPY CivicJobs.Jobs.Api/CivicJobs.Jobs.Api.csproj CivicJobs.Jobs.Api/
RUN dotnet restore CivicJobs.Jobs.Api/CivicJobs.Jobs.Api.csproj

COPY CivicJobs.Core/ CivicJobs.Core/
COPY CivicJobs.Application/ CivicJobs.Application/
COPY CivicJobs.Infrastructure/ CivicJobs.Infrastructure/
COPY CivicJobs.Jobs.Api/ CivicJobs.Jobs.Api/
RUN dotnet publish CivicJobs.Jobs.Api/CivicJobs.Jobs.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# SQLite needs to write the database (plus its -wal and -shm files) in /data as the non-root app user.
# A new named volume mounted at /data copies this ownership.
RUN mkdir -p /data && chown $APP_UID /data

COPY --from=build /app/publish .

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "CivicJobs.Jobs.Api.dll"]