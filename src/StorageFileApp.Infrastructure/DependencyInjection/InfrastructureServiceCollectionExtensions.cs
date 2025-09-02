using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        // Repositories
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();
        services.AddScoped<IStorageProviderRepository, StorageProviderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Storage Services
        services.AddScoped<IStorageService, FileSystemStorageService>();

        // Domain Event Publisher
        services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

        // MassTransit with RabbitMQ
        services.AddMassTransitWithRabbitMq(configuration);

        // Seed Data
        services.AddScoped<IHostedService, SeedDataService>();

        return services;
    }
}
