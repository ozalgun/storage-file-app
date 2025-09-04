using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Events;
using StorageFileApp.Infrastructure.Data;
using StorageFileApp.Infrastructure.Events;
using StorageFileApp.Infrastructure.Messaging;
using StorageFileApp.Infrastructure.Repositories;
using StorageFileApp.Infrastructure.Services;

namespace StorageFileApp.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database Context
        services.AddDbContext<StorageFileDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Port=5432;Database=StorageFileApp;Username=storageuser;Password=storagepass123;Include Error Detail=true;";
            options.UseNpgsql(connectionString);
        });

        // DbContext Factory for repositories
        services.AddDbContextFactory<StorageFileDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? "Host=localhost;Port=5432;Database=StorageFileApp;Username=storageuser;Password=storagepass123;Include Error Detail=true;";
            options.UseNpgsql(connectionString);
        });

        // Repositories
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();
        services.AddScoped<IStorageProviderRepository, StorageProviderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Storage Services - Register concrete implementations
        services.AddScoped<FileSystemStorageService>(provider => 
        {
            var logger = provider.GetRequiredService<ILogger<FileSystemStorageService>>();
            return new FileSystemStorageService(logger, "storage");
        });
        
        services.AddScoped<MinioS3StorageService>(provider =>
        {
            var logger = provider.GetRequiredService<ILogger<MinioS3StorageService>>();
            var s3Client = provider.GetRequiredService<IAmazonS3>();
            return new MinioS3StorageService(logger, s3Client, "storage-file-app");
        });
        
        // Register IStorageService with default implementation
        services.AddScoped<IStorageService>(provider => 
            provider.GetRequiredService<FileSystemStorageService>());
        
        // Storage Provider Factory and Strategy
        services.AddScoped<IStorageProviderFactory, StorageProviderFactory>();
        services.AddScoped<IStorageStrategyService, StorageStrategyService>();
        
        // AWS S3 Configuration
        services.AddSingleton<IAmazonS3>(provider =>
        {
            var config = new AmazonS3Config
            {
                ServiceURL = configuration["MinIO:ServiceURL"] ?? "http://localhost:9000",
                ForcePathStyle = true, // Required for MinIO
                UseHttp = true
            };
            
            var credentials = new Amazon.Runtime.BasicAWSCredentials(
                configuration["MinIO:AccessKey"] ?? "minioadmin",
                configuration["MinIO:SecretKey"] ?? "minioadmin123"
            );
            
            return new AmazonS3Client(credentials, config);
        });

        // Domain Event Publisher
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        // MassTransit with RabbitMQ
        services.AddMassTransitWithRabbitMq(configuration);

        // Seed Data
                services.AddScoped<IHostedService, SeedDataService>();
        services.AddScoped<IMessageQueueHealthService, MessageQueueHealthService>();
        
        return services;
    }
}
