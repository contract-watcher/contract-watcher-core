using ContractWatcher.Core.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Data;

public class DataContext(DbContextOptions options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Integration> Integrations => Set<Integration>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<ContractVersion> ContractVersions => Set<ContractVersion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DataContext).Assembly);
    }
}
