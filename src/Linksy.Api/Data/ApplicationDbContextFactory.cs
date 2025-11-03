using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Linksy.Api.Data;

/// <summary>
/// Design-time factory for creating ApplicationDbContext instances
/// Used by EF Core tooling for migrations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        // Use a temporary connection string for design-time operations
        // This allows 'dotnet ef migrations add' to work without a running database
        var connectionString = "Host=localhost;Database=linksy_design;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        });

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
