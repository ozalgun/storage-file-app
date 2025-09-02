using StorageFileApp.Domain.Enums;

namespace StorageFileApp.Domain.Entities.StorageProviderEntity;

public class StorageProvider
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public StorageProviderType Type { get; private set; }
    public string ConnectionString { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    private StorageProvider() { } 
    
    public StorageProvider(string name, StorageProviderType type, string connectionString)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type;
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateName(string name)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void UpdateConnectionString(string connectionString)
    {
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        UpdatedAt = DateTime.UtcNow;
    }
}
