using Microsoft.OpenApi;

namespace TaskManager.Api.Swagger;

public static class SwaggerExtensions
{
    private const string SchemeId = "Bearer";

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Task Manager API", Version = "v1" });

            // Adds the "Authorize" button; paste the raw token (no "Bearer " prefix).
            options.AddSecurityDefinition(SchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Paste a JWT. In Development, get one from POST /dev/token."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(SchemeId, document)] = []
            });

            foreach (var xml in Directory.GetFiles(AppContext.BaseDirectory, "TaskManager.*.xml"))
            {
                options.IncludeXmlComments(xml);
            }
        });

        return services;
    }
}
