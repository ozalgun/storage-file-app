namespace StorageFileApp.Domain.Enums;

public enum FileStatus
{
    Pending,        // Dosya yüklendi, işlenmeyi bekliyor
    Processing,     // Dosya chunk'lara ayrılıyor
    Chunked,        // Dosya chunk'lara ayrıldı
    Available,      // Dosya kullanıma hazır
    Stored,         // Tüm chunk'lar storage'lara kaydedildi
    Failed,         // İşlem sırasında hata oluştu
    Error,          // İşlem sırasında hata oluştu (deprecated)
    Deleted         // Dosya silindi
}
