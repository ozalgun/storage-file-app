namespace StorageFileApp.Domain.Entities.StorageProviderEntity;

public class StorageProvider
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string ConnectionString { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    private StorageProvider() { } 
    
    public StorageProvider(string name, string type, string connectionString)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }
    
    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
