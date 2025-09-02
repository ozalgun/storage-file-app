using StorageFileApp.Application.DTOs;
using StorageFileApp.Application.UseCases;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace StorageFileApp.Application.Services;

public class StorageProviderApplicationService(
    ILogger<StorageProviderApplicationService> logger,
    IStorageProviderRepository storageProviderRepository,
    IUnitOfWork unitOfWork)
    : IStorageProviderUseCase
{
    private readonly ILogger<StorageProviderApplicationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IStorageProviderRepository _storageProviderRepository = storageProviderRepository ?? throw new ArgumentNullException(nameof(storageProviderRepository));
    private readonly IUnitOfWork _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));

    public async Task<StorageProviderResult> RegisterStorageProviderAsync(RegisterStorageProviderRequest request)
    {
        try
        {
            _logger.LogInformation("Registering storage provider: {Name} of type {Type}", 
                request.Name, request.Type);
            
            var provider = new StorageProvider(request.Name, request.Type, request.ConnectionString);
            
            await _storageProviderRepository.AddAsync(provider);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Storage provider registered successfully: {Name} with ID: {Id}", 
                request.Name, provider.Id);
            
            return new StorageProviderResult(true, Provider: new StorageProviderInfo(
                provider.Id, provider.Name, provider.Type, provider.IsActive, 
                provider.CreatedAt, provider.UpdatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while registering storage provider: {Name}", request.Name);
            return new StorageProviderResult(false, ErrorMessage: ex.Message);
        }
    }
    
    public async Task<StorageProviderResult> UpdateStorageProviderAsync(UpdateStorageProviderRequest request)
    {
        try
        {
            _logger.LogInformation("Updating storage provider ID: {Id}", request.Id);
            
            var provider = await _storageProviderRepository.GetByIdAsync(request.Id);
            if (provider == null)
            {
                return new StorageProviderResult(false, ErrorMessage: "Storage provider not found");
            }
            
            provider.UpdateName(request.Name ?? throw new ArgumentNullException(nameof(request.Name)));
            provider.UpdateConnectionString(request.ConnectionString ?? throw new ArgumentNullException(nameof(request.ConnectionString)));
            provider.SetActive(request.IsActive ?? true);
            
            await _storageProviderRepository.UpdateAsync(provider);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Storage provider updated successfully: {Id}", request.Id);
            
            return new StorageProviderResult(true, Provider: new StorageProviderInfo(
                provider.Id, provider.Name, provider.Type, provider.IsActive, 
                provider.CreatedAt, provider.UpdatedAt));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating storage provider ID: {Id}", request.Id);
            return new StorageProviderResult(false, ErrorMessage: ex.Message);
        }
    }
    
    public async Task<StorageProviderDeletionResult> DeleteStorageProviderAsync(DeleteStorageProviderRequest request)
    {
        try
        {
            _logger.LogInformation("Deleting storage provider ID: {Id}", request.Id);
            
            var provider = await _storageProviderRepository.GetByIdAsync(request.Id);
            if (provider == null)
            {
                return new StorageProviderDeletionResult(false, ErrorMessage: "Storage provider not found");
            }
            
            // Check if provider is in use by any chunks
            var chunksUsingProvider = await _storageProviderRepository.GetChunkCountByProviderIdAsync(request.Id);
            if (chunksUsingProvider > 0 && !request.ForceDelete)
            {
                return new StorageProviderDeletionResult(false, 
                    ErrorMessage: $"Cannot delete storage provider. It is being used by {chunksUsingProvider} chunks. Use ForceDelete to override.");
            }
            
            await _storageProviderRepository.DeleteAsync(provider);
            await _unitOfWork.SaveChangesAsync();
            
            _logger.LogInformation("Storage provider deleted successfully: {Id}", request.Id);
            
            return new StorageProviderDeletionResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting storage provider ID: {Id}", request.Id);
            return new StorageProviderDeletionResult(false, ErrorMessage: ex.Message);
        }
    }
    
    public async Task<StorageProviderListResult> ListStorageProvidersAsync()
    {
        try
        {
            _logger.LogInformation("Listing storage providers");
            
            var providers = await _storageProviderRepository.GetAllAsync();
            var providerInfos = providers.Select(p => new StorageProviderInfo(
                p.Id, p.Name, p.Type, p.IsActive, p.CreatedAt, p.UpdatedAt)).ToList();
            
            return new StorageProviderListResult(true, Providers: providerInfos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while listing storage providers");
            return new StorageProviderListResult(false, ErrorMessage: ex.Message);
        }
    }
    
    public async Task<StorageProviderHealthResult> CheckStorageProviderHealthAsync(CheckStorageProviderHealthRequest request)
    {
        try
        {
            _logger.LogInformation("Checking health of storage provider ID: {Id}", request.Id);
            
            var provider = await _storageProviderRepository.GetByIdAsync(request.Id);
            if (provider == null)
            {
                return new StorageProviderHealthResult(false, ErrorMessage: "Storage provider not found");
            }
            
            // Basic health check - in a real implementation, this would test the connection
            var isHealthy = provider.IsActive; // Simplified health check
            
            var healthInfo = new StorageProviderHealthInfo(
                Id: provider.Id,
                Name: provider.Name,
                Type: provider.Type,
                IsHealthy: isHealthy,
                IsActive: provider.IsActive,
                CheckedAt: DateTime.UtcNow,
                ErrorMessage: isHealthy ? null : "Provider is inactive"
            );
            
            return new StorageProviderHealthResult(true, Providers: [healthInfo]);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking health of storage provider ID: {Id}", request.Id);
            return new StorageProviderHealthResult(false, ErrorMessage: ex.Message);
        }
    }
}