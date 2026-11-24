using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace ClaimsPortal.Claims.Infrastructure.Persistence;

/// <summary>
/// Used by `dotnet ef` so migrations can be created without starting the API.
/// ConnectionStrings__DefaultConnection overrides the development appsettings value.
/// </summary>
public sealed class ClaimsDbContextFactory : IDesignTimeDbContextFactory<ClaimsDbContext>
{
    public ClaimsDbContext CreateDbContext(string[] args)
    {
        var apiDirectory = FindApiDirectory();
        var configuration = new ConfigurationBuilder()
            .SetBasePath(apiDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets("e4b4de31-e98a-4965-9b9a-6b7a7ea18b5e")
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing. Set ConnectionStrings__DefaultConnection before running dotnet ef.");
        }

        var options = new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ClaimsDbContext(options);
    }

    private static string FindApiDirectory()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "src", "ClaimsPortal.Claims.Api");
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
            {
                return candidate;
            }

            var nested = Path.Combine(current.FullName, "backend", "src", "ClaimsPortal.Claims.Api");
            if (File.Exists(Path.Combine(nested, "appsettings.json")))
            {
                return nested;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate the API appsettings.json for design-time migrations.");
    }
}
