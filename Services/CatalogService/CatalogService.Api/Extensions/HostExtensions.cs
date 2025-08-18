using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace CatalogService.Api.Extensions;

public static class HostExtension
{
    public static async Task<IHost> MigrateDbContextAsync<TContext>(this IHost host, Func<TContext, IServiceProvider, Task> seeder) where TContext : DbContext
    {
        using var scope = host.Services.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<TContext>>();
        var context = services.GetRequiredService<TContext>();

        try
        {
            logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);

            var retry = Policy
                .Handle<SqlException>()
                .WaitAndRetryAsync(new[]
                {
                    TimeSpan.FromSeconds(3),
                    TimeSpan.FromSeconds(5),
                    TimeSpan.FromSeconds(8)
                });

            await retry.ExecuteAsync(async () =>
            {
                await InvokeSeederAsync(seeder, context, services);
            });

            logger.LogInformation("Migrated database associated with context {DbContextName}", typeof(TContext).Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "An error occurred while migrating the database used on context {DbContextName}",
                typeof(TContext).Name);
        }

        return host;
    }

    private static async Task InvokeSeederAsync<TContext>(Func<TContext, IServiceProvider, Task> seeder, TContext context, IServiceProvider services) where TContext : DbContext
    {
        // Ensure + migrate
        await context.Database.EnsureCreatedAsync();
        await context.Database.MigrateAsync();

        // Run custom seeder
        await seeder(context, services);
    }
}
