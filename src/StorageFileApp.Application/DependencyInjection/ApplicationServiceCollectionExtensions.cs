using Microsoft.Extensions.DependencyInjection;
using StorageFileApp.Application.Services;
using StorageFileApp.Application.UseCases;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Services;

namespace StorageFileApp.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register Use Cases
        services.AddScoped<IFileStorageUseCase, FileStorageApplicationService>();
        services.AddScoped<IFileChunkingUseCase, FileChunkingApplicationService>();
        services.AddScoped<IStorageProviderUseCase, StorageProviderApplicationService>();
        services.AddScoped<IFileHealthUseCase, FileHealthApplicationService>();
        
        // Register Domain Services
        services.AddScoped<IFileChunkingDomainService, FileChunkingDomainService>();
        services.AddScoped<IFileIntegrityDomainService, FileIntegrityDomainService>();
        services.AddScoped<IFileMergingDomainService, FileMergingDomainService>();
        services.AddScoped<IStorageStrategyDomainService, StorageStrategyDomainService>();
        services.AddScoped<IChunkHealthDomainService, ChunkHealthDomainService>();
        services.AddScoped<IFileValidationDomainService, FileValidationDomainService>();
        services.AddScoped<IChunkOptimizationDomainService, ChunkOptimizationDomainService>();
        
        // Note: Repository and Storage Service implementations will be registered in Infrastructure layer
        // services.AddScoped<IFileRepository, FileRepository>();
        // services.AddScoped<IChunkRepository, ChunkRepository>();
        // services.AddScoped<IStorageProviderRepository, StorageProviderRepository>();
        // services.AddScoped<IStorageService, StorageService>();
        // services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        return services;
    }
}
