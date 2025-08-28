namespace StorageFileApp.Domain.Enums;

public enum ChunkStatus
{
    Pending,        // Chunk oluşturuldu, işlenmeyi bekliyor
    Storing,        // Chunk storage'a yazılıyor
    Stored,         // Chunk başarıyla storage'a yazıldı
    Error,          // Chunk storage'a yazılırken hata oluştu
    Deleted         // Chunk silindi
}
