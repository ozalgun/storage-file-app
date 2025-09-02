namespace StorageFileApp.Domain.Enums;

public enum ChunkStatus
{
    Pending,        // Chunk oluşturuldu, işlenmeyi bekliyor
    Processing,     // Chunk işleniyor
    Storing,        // Chunk storage'a yazılıyor
    Stored,         // Chunk başarıyla storage'a yazıldı
    Failed,         // Chunk storage'a yazılırken hata oluştu
    Error,          // Chunk storage'a yazılırken hata oluştu (deprecated)
    Deleted         // Chunk silindi
}
