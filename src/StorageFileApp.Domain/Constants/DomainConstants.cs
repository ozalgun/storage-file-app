namespace StorageFileApp.Domain.Constants;

public static class DomainConstants
{
    // File size limits
    public const long MIN_FILE_SIZE = 1; // 1 byte
    public const long MAX_FILE_SIZE = 10L * 1024 * 1024 * 1024; // 10GB
    
    // Chunk size limits
    public const long MIN_CHUNK_SIZE = 64 * 1024; // 64KB
    public const long MAX_CHUNK_SIZE = 100 * 1024 * 1024; // 100MB
    public const int MAX_CHUNK_COUNT = 10000;
    
    // File name limits
    public const int MIN_FILE_NAME_LENGTH = 1;
    public const int MAX_FILE_NAME_LENGTH = 255;
    
    // Storage provider limits
    public const int MAX_STORAGE_PROVIDERS = 10;
    public const int MIN_STORAGE_PROVIDERS = 1;
    public const int MAX_CONCURRENT_OPERATIONS_PER_PROVIDER = 100;
    
    
    // Default values
    public const int DEFAULT_CHUNK_SIZE = 1024 * 1024; // 1MB
    public const int DEFAULT_RETRY_COUNT = 3;
    public const int DEFAULT_TIMEOUT_SECONDS = 30;
    
    // File type restrictions
    public static readonly string[] FORBIDDEN_FILE_EXTENSIONS = 
    {
        ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".jar", ".app"
    };
    
    public static readonly string[] ALLOWED_FILE_EXTENSIONS = 
    {
        ".txt", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".svg",
        ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm",
        ".mp3", ".wav", ".flac", ".aac", ".ogg",
        ".zip", ".rar", ".7z", ".tar", ".gz"
    };
    
    // Content type mappings
    public static readonly Dictionary<string, string[]> CONTENT_TYPE_EXTENSIONS = new()
    {
        ["text/plain"] = [".txt"],
        ["application/pdf"] = [".pdf"],
        ["application/msword"] = [".doc"],
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = [".docx"],
        ["image/jpeg"] = [".jpg", ".jpeg"],
        ["image/png"] = [".png"],
        ["image/gif"] = [".gif"],
        ["video/mp4"] = [".mp4"],
        ["audio/mpeg"] = [".mp3"],
        ["application/zip"] = [".zip"]
    };
    
    // Error messages
    public const string ERROR_FILE_TOO_LARGE = "File size exceeds maximum allowed size";
    public const string ERROR_FILE_TOO_SMALL = "File size is below minimum allowed size";
    public const string ERROR_INVALID_FILE_NAME = "File name contains invalid characters";
    public const string ERROR_UNSUPPORTED_FILE_TYPE = "File type is not supported";
    public const string ERROR_NO_STORAGE_PROVIDERS = "No active storage providers available";
    public const string ERROR_CHUNK_CORRUPTED = "Chunk data is corrupted";
    public const string ERROR_REPLICATION_FAILED = "Chunk replication failed";
}
