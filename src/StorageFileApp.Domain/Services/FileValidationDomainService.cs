using StorageFileApp.Domain.ValueObjects;
using StorageFileApp.Domain.Constants;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Services;

public class FileValidationDomainService : IFileValidationDomainService
{
    
    private readonly string[] _forbiddenCharacters = { "<", ">", ":", "\"", "|", "?", "*", "\\", "/" };
    
    public Task<bool> ValidateFileNameAsync(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Task.FromResult(false);
            
        if (fileName.Length < DomainConstants.MIN_FILE_NAME_LENGTH || fileName.Length > DomainConstants.MAX_FILE_NAME_LENGTH)
            return Task.FromResult(false);
            
        if (_forbiddenCharacters.Any(c => fileName.Contains(c)))
            return Task.FromResult(false);
            
        // Check for reserved names (Windows)
        var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).ToUpper();
        
        if (reservedNames.Contains(nameWithoutExtension))
            return Task.FromResult(false);
            
        return Task.FromResult(true);
    }
    
    public Task<bool> ValidateFileSizeAsync(long fileSize)
    {
        return Task.FromResult(fileSize >= DomainConstants.MIN_FILE_SIZE && fileSize <= DomainConstants.MAX_FILE_SIZE);
    }
    
    public Task<bool> ValidateFileTypeAsync(string fileName, IEnumerable<string> allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return Task.FromResult(false);
            
        var extension = Path.GetExtension(fileName).ToLower();
        
        // Check if file extension is forbidden
        if (DomainConstants.FORBIDDEN_FILE_EXTENSIONS.Contains(extension))
            return Task.FromResult(false);
            
        var allowedExtensionsList = allowedExtensions.Select(e => e.ToLower()).ToList();
        
        return Task.FromResult(allowedExtensionsList.Contains(extension));
    }
    
    public Task<bool> ValidateFileMetadataAsync(FileMetadata metadata)
    {
        if (metadata == null)
            return Task.FromResult(false);
            
        if (string.IsNullOrWhiteSpace(metadata.ContentType))
            return Task.FromResult(false);
            
        // Validate content type format
        if (!IsValidContentType(metadata.ContentType))
            return Task.FromResult(false);
            
        return Task.FromResult(true);
    }
    
    public Task<ValidationResult> ValidateFileForStorageAsync(FileEntity file)
    {
        var result = new ValidationResult { IsValid = true };
        
        // Validate file name
        if (!ValidateFileNameAsync(file.Name).Result)
        {
            result.IsValid = false;
            result.Errors.Add($"Invalid file name: {file.Name}");
        }
        
        // Validate file size
        if (!ValidateFileSizeAsync(file.Size).Result)
        {
            result.IsValid = false;
            result.Errors.Add($"{DomainConstants.ERROR_FILE_TOO_LARGE}: {file.Size} bytes is out of allowed range ({DomainConstants.MIN_FILE_SIZE} - {DomainConstants.MAX_FILE_SIZE} bytes)");
        }
        
        // Validate file type
        if (!ValidateFileTypeAsync(file.Name, DomainConstants.ALLOWED_FILE_EXTENSIONS).Result)
        {
            result.Warnings.Add($"File type {Path.GetExtension(file.Name)} may not be supported");
        }
        
        // Validate metadata
        if (!ValidateFileMetadataAsync(file.Metadata).Result)
        {
            result.IsValid = false;
            result.Errors.Add("Invalid file metadata");
        }
        
        // Validate checksum
        if (string.IsNullOrWhiteSpace(file.Checksum))
        {
            result.IsValid = false;
            result.Errors.Add("File checksum is required");
        }
        
        return Task.FromResult(result);
    }
    
    private bool IsValidContentType(string contentType)
    {
        // Basic content type validation
        if (string.IsNullOrWhiteSpace(contentType))
            return false;
            
        // Should contain at least one slash
        if (!contentType.Contains('/'))
            return false;
            
        // Should not contain spaces or special characters
        if (contentType.Contains(' ') || contentType.Contains('\t') || contentType.Contains('\n'))
            return false;
            
        return true;
    }
    
    public Task<IEnumerable<string>> GetSupportedExtensionsAsync()
    {
        return Task.FromResult<IEnumerable<string>>(DomainConstants.ALLOWED_FILE_EXTENSIONS);
    }
    
    public Task<long> GetMaxFileSizeAsync()
    {
        return Task.FromResult(DomainConstants.MAX_FILE_SIZE);
    }
    
    public Task<long> GetMinFileSizeAsync()
    {
        return Task.FromResult(DomainConstants.MIN_FILE_SIZE);
    }
}
