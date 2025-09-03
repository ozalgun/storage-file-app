using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Consumers;
using StorageFileApp.Application.Contracts;

namespace StorageFileApp.Infrastructure.Messaging;

public static class MassTransitConfiguration
{
    public static IServiceCollection AddMassTransitWithRabbitMq(this IServiceCollection services, IConfiguration configuration)
    {
        var rabbitMqHost = configuration["RabbitMQ:Host"] ?? "localhost";
        var rabbitMqPort = configuration["RabbitMQ:Port"] ?? "5672";
        var rabbitMqUsername = configuration["RabbitMQ:Username"] ?? "storageuser";
        var rabbitMqPassword = configuration["RabbitMQ:Password"] ?? "storagepass123";
        var rabbitMqVirtualHost = configuration["RabbitMQ:VirtualHost"] ?? "storage-vhost";

        services.AddMassTransit(x =>
        {
            // Add all consumers
            x.AddConsumer<FileCreatedEventConsumer>();
            x.AddConsumer<FileStatusChangedEventConsumer>();
            x.AddConsumer<FileDeletedEventConsumer>();
            x.AddConsumer<ChunkCreatedEventConsumer>();
            x.AddConsumer<ChunkStatusChangedEventConsumer>();
            x.AddConsumer<ChunkStoredEventConsumer>();

            // Configure RabbitMQ
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, rabbitMqPort, rabbitMqVirtualHost, h =>
                {
                    h.Username(rabbitMqUsername);
                    h.Password(rabbitMqPassword);
                });

                // Configure message routing
                ConfigureMessageRouting(cfg);

                // Configure consumers
                ConfigureConsumers(cfg, context);
            });
        });

        // MassTransit hosted service is automatically registered in newer versions

        return services;
    }

    private static void ConfigureMessageRouting(IRabbitMqBusFactoryConfigurator cfg)
    {
        // File Events
        cfg.Message<FileCreatedEvent>(e => e.SetEntityName("file.created"));
        cfg.Message<FileStatusChangedEvent>(e => e.SetEntityName("file.status.changed"));
        cfg.Message<FileDeletedEvent>(e => e.SetEntityName("file.deleted"));

        // Chunk Events
        cfg.Message<ChunkCreatedEvent>(e => e.SetEntityName("chunk.created"));
        cfg.Message<ChunkStatusChangedEvent>(e => e.SetEntityName("chunk.status.changed"));
        cfg.Message<ChunkStoredEvent>(e => e.SetEntityName("chunk.stored"));

        // Storage Provider Events
        cfg.Message<StorageProviderHealthCheckEvent>(e => e.SetEntityName("storage.health.check"));
        cfg.Message<StorageProviderSpaceWarningEvent>(e => e.SetEntityName("storage.space.warning"));
    }

    private static void ConfigureConsumers(IRabbitMqBusFactoryConfigurator cfg, IBusRegistrationContext context)
    {
        // File Event Consumers
        cfg.ReceiveEndpoint("file-created-queue", e =>
        {
            e.ConfigureConsumer<FileCreatedEventConsumer>(context);
            e.Bind("file.created");
        });

        cfg.ReceiveEndpoint("file-status-changed-queue", e =>
        {
            e.ConfigureConsumer<FileStatusChangedEventConsumer>(context);
            e.Bind("file.status.changed");
        });

        cfg.ReceiveEndpoint("file-deleted-queue", e =>
        {
            e.ConfigureConsumer<FileDeletedEventConsumer>(context);
            e.Bind("file.deleted");
        });

        // Chunk Event Consumers
        cfg.ReceiveEndpoint("chunk-created-queue", e =>
        {
            e.ConfigureConsumer<ChunkCreatedEventConsumer>(context);
            e.Bind("chunk.created");
        });

        cfg.ReceiveEndpoint("chunk-status-changed-queue", e =>
        {
            e.ConfigureConsumer<ChunkStatusChangedEventConsumer>(context);
            e.Bind("chunk.status.changed");
        });

        cfg.ReceiveEndpoint("chunk-stored-queue", e =>
        {
            e.ConfigureConsumer<ChunkStoredEventConsumer>(context);
            e.Bind("chunk.stored");
        });

        // Storage Provider Event Consumers
        cfg.ReceiveEndpoint("storage-health-check-queue", e =>
        {
            e.Bind("storage.health.check");
            // Note: Storage provider health check consumers would be added here
        });

        cfg.ReceiveEndpoint("storage-space-warning-queue", e =>
        {
            e.Bind("storage.space.warning");
            // Note: Storage provider space warning consumers would be added here
        });
    }
}
