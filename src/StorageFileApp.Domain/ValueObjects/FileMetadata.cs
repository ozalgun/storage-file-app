namespace StorageFileApp.Domain.ValueObjects;

public class FileMetadata
{
    public string ContentType { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Dictionary<string, string> CustomProperties { get; private set; } = new();
    
    private FileMetadata() { }
    
    public FileMetadata(string contentType, string? description = null)
    {
        ContentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        Description = description;
        CustomProperties = new Dictionary<string, string>();
    }
    
    public void AddCustomProperty(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Key cannot be null or empty", nameof(key));
            
        CustomProperties[key] = value;
    }
    
    public void RemoveCustomProperty(string key)
    {
        if (CustomProperties.ContainsKey(key))
            CustomProperties.Remove(key);
    }
}
