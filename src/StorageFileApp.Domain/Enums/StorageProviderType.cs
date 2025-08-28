namespace StorageFileApp.Domain.Enums;

public enum StorageProviderType
{
    FileSystem,     // Yerel dosya sistemi
    Database,       // Veritabanı storage
    CloudStorage,   // Bulut storage (AWS S3, Azure Blob)
    NetworkStorage  // Ağ storage (NAS, SAN)
}
