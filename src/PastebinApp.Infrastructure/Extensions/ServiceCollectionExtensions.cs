using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using PastebinApp.Application.Interfaces;
using PastebinApp.Infrastructure.Caching;
using PastebinApp.Infrastructure.Persistence;
using PastebinApp.Infrastructure.Persistence.Repositories;
using PastebinApp.Infrastructure.Storage;
using StackExchange.Redis;

namespace PastebinApp.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorCodesToAdd: null);
            });

            // Для development
            if (configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Redis
        var redisConnection = configuration.GetConnectionString("Redis");
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = "PastebinApp:";
        });

        services.AddSingleton<IConnectionMultiplexer>(sp =>
        {
            return ConnectionMultiplexer.Connect(redisConnection!);
        });

        // MinIO (S3-compatible storage)
        var minioEndpoint = configuration["MinIO:Endpoint"] ?? "localhost:9000";
        var minioAccessKey = configuration["MinIO:AccessKey"] ?? "minioadmin";
        var minioSecretKey = configuration["MinIO:SecretKey"] ?? "minioadmin123";
        var minioUseSSL = configuration.GetValue<bool>("MinIO:UseSSL", false);

        services.AddSingleton<IMinioClient>(sp =>
        {
            return new MinioClient()
                .WithEndpoint(minioEndpoint)
                .WithCredentials(minioAccessKey, minioSecretKey)
                .WithSSL(minioUseSSL)
                .Build();
        });

        services.AddScoped<IPasteRepository, PasteRepository>();

        services.AddScoped<ICacheService, RedisCacheService>();

        services.AddScoped<IBlobStorageService, MinIoBlobStorageService>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}