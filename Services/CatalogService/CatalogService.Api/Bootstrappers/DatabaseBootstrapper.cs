using CatalogService.Api.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Api.Bootstrappers;

public static class DatabaseBootstrapper
{
    public static IServiceCollection AddDatabase(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<CatalogContext>(options =>
        {
            options.UseSqlServer(
                configuration.GetConnectionString("CatalogConnection"),
                sqlOptions =>
                {
                    sqlOptions.MigrationsAssembly(typeof(CatalogContext).Assembly.FullName);
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 15,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
        });

        return services;
    }
}