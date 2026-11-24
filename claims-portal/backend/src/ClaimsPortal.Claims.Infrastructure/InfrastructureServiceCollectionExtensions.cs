using System.Text;
using ClaimsPortal.Claims.Application.Abstractions;
using ClaimsPortal.Claims.Infrastructure.Events;
using ClaimsPortal.Claims.Infrastructure.Persistence;
using ClaimsPortal.Claims.Infrastructure.Security;
using ClaimsPortal.Claims.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace ClaimsPortal.Claims.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddClaimsInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing. Set it in appsettings or the ConnectionStrings__DefaultConnection environment variable.");
        }

        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key must be at least 32 bytes. Set Jwt:Key or the Jwt__Key environment variable. Do not reuse a development key outside this demo.");
        }

        services.AddDbContext<ClaimsDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IClaimsStore, EfClaimsStore>();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        services.AddScoped<IClaimEventPublisher, LoggingClaimEventPublisher>();

        var relativePath = configuration["Storage:UploadPath"] ?? "uploads";
        var root = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(environment.ContentRootPath, relativePath);
        services.AddSingleton<IFileStorage>(_ => new LocalFileStorage(root));

        return services;
    }
}
