using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Infrastructure.Data;
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
                ?? "Server=(localdb)\\mssqllocaldb;Database=StorageFileApp;Trusted_Connection=true;MultipleActiveResultSets=true";
            options.UseSqlServer(connectionString);
        });

        // Repositories
        services.AddScoped<IFileRepository, FileRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();
        services.AddScoped<IStorageProviderRepository, StorageProviderRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Storage Services
        services.AddScoped<IStorageService, FileSystemStorageService>();

        return services;
    }
}
