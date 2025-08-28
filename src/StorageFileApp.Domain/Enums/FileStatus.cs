namespace StorageFileApp.Domain.Enums;

public enum FileStatus
{
    Pending,        // Dosya yüklendi, işlenmeyi bekliyor
    Processing,     // Dosya chunk'lara ayrılıyor
    Chunked,        // Dosya chunk'lara ayrıldı
    Stored,         // Tüm chunk'lar storage'lara kaydedildi
    Error,          // İşlem sırasında hata oluştu
    Deleted         // Dosya silindi
}
