namespace StorageFileApp.Domain.Entities.FileEntity;

public class File
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public long Size { get; set; }
    public required string Checksum { get; set; }
    public required string Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    
    private File() { } 
    
    public File(string name, long size, string checksum)
    {
        Id = Guid.NewGuid();
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Size = size;
        Checksum = checksum ?? throw new ArgumentNullException(nameof(checksum));
        Status = "Pending";
        CreatedAt = DateTime.UtcNow;
    }
    
    public void UpdateStatus(string status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }
}
