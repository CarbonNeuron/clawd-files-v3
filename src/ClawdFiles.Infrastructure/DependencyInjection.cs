using ClawdFiles.Application.Interfaces;
using ClawdFiles.Infrastructure.Data;
using ClawdFiles.Infrastructure.Repositories;
using ClawdFiles.Infrastructure.Security;
using ClawdFiles.Infrastructure.Services;
using ClawdFiles.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ClawdFiles.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ClawdFilesDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApiKeyRepository, EfApiKeyRepository>();
        services.AddScoped<IBucketRepository, EfBucketRepository>();
        services.AddScoped<IFileHeaderRepository, EfFileHeaderRepository>();

        var storagePath = configuration.GetValue<string>("Storage:RootPath") ?? "./storage";
        services.AddSingleton<IFileStorage>(new LocalFileStorage(storagePath));

        services.AddSingleton<IApiKeyHasher, Sha256ApiKeyHasher>();
        services.AddScoped<IShortCodeGenerator, RandomShortCodeGenerator>();

        services.Configure<ExpiryOptions>(configuration.GetSection("Expiry"));
        services.AddHostedService<BucketExpiryService>();

        return services;
    }
}
