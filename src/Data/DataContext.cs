using Microsoft.EntityFrameworkCore;

namespace ContractWatcher.Core.Data;

public class DataContext(DbContextOptions options) : DbContext(options)
{
    // DbSet ...
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Model Configurations ...
    }
}