using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Extensions;

public static class HostExtension
{
    public static void PrepareAndRun<TContext>(this IHost host, string[] args) where TContext : DbContext
    {
        if (args.Contains("--migrate"))
        {
            using var scope = host.Services.CreateScope();
            using var context = scope.ServiceProvider.GetRequiredService<TContext>();
            
            if (context.Database.GetPendingMigrations().Any())
            {
                context.Database.SetCommandTimeout(3000);
                context.Database.Migrate();
            }
        }

        host.Run();
    }
}