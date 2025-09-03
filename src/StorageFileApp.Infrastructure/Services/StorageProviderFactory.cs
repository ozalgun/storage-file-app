using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Infrastructure.Services;

public class StorageProviderFactory : IStorageProviderFactory
{
    private readonly ILogger<StorageProviderFactory> _logger;
    private readonly IConfiguration _configuration;
    private readonly FileSystemStorageService _fileSystemService;
    private readonly MinioS3StorageService _s3Service;

    public StorageProviderFactory(
        ILogger<StorageProviderFactory> logger,
        IConfiguration configuration,
        FileSystemStorageService fileSystemService,
        MinioS3StorageService s3Service)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _fileSystemService = fileSystemService ?? throw new ArgumentNullException(nameof(fileSystemService));
        _s3Service = s3Service ?? throw new ArgumentNullException(nameof(s3Service));
    }

    public IStorageService GetStorageService(StorageProvider provider)
    {
        _logger.LogInformation("Selecting storage service for provider: {ProviderName} (Type: {ProviderType})", 
            provider.Name, provider.Type);

        return provider.Type switch
        {
            StorageProviderType.FileSystem => _fileSystemService,
            StorageProviderType.MinIO => _s3Service, // MinIO is S3-compatible
            _ => throw new NotSupportedException($"Storage provider type '{provider.Type}' is not supported")
        };
    }

    public IStorageService GetDefaultStorageService()
    {
        var defaultProvider = _configuration["StorageSettings:DefaultProvider"] ?? "FileSystem";
        _logger.LogInformation("Using default storage provider: {DefaultProvider}", defaultProvider);

        return defaultProvider switch
        {
            "MinIO" => _s3Service,
            "FileSystem" => _fileSystemService,
            _ => _fileSystemService // Fallback to FileSystem
        };
    }

    public IEnumerable<IStorageService> GetAllStorageServices()
    {
        var enabledProviders = new List<IStorageService>();

        if (bool.Parse(_configuration["StorageSettings:FileSystemEnabled"] ?? "true"))
        {
            enabledProviders.Add(_fileSystemService);
        }

        if (bool.Parse(_configuration["StorageSettings:MinIOEnabled"] ?? "false"))
        {
            enabledProviders.Add(_s3Service);
        }

        _logger.LogInformation("Available storage services: {ServiceCount}", enabledProviders.Count);
        return enabledProviders;
    }
}
