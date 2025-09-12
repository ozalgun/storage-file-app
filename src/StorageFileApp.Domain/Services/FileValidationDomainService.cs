using StorageFileApp.Domain.ValueObjects;
using StorageFileApp.Domain.Constants;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Services;

public class FileValidationDomainService : IFileValidationDomainService
{
    
    private readonly string[] _forbiddenCharacters = { "<", ">", ":", "\"", "|", "?", "*", "\\", "/" };
    
    private bool ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;
            
        if (fileName.Length < DomainConstants.MIN_FILE_NAME_LENGTH || fileName.Length > DomainConstants.MAX_FILE_NAME_LENGTH)
            return false;
            
        if (_forbiddenCharacters.Any(c => fileName.Contains(c)))
            return false;
            
        // Check for reserved names (Windows)
        var reservedNames = new[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName).ToUpper();
        
        if (reservedNames.Contains(nameWithoutExtension))
            return false;
            
        return true;
    }
    
    private bool ValidateFileSize(long fileSize)
    {
        return fileSize >= DomainConstants.MIN_FILE_SIZE && fileSize <= DomainConstants.MAX_FILE_SIZE;
    }
    
    private bool ValidateFileType(string fileName, IEnumerable<string> allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return false;
            
        var extension = Path.GetExtension(fileName).ToLower();
        
        // Check if file extension is forbidden
        if (DomainConstants.FORBIDDEN_FILE_EXTENSIONS.Contains(extension))
            return false;
            
        var allowedExtensionsList = allowedExtensions.Select(e => e.ToLower()).ToList();
        
        return allowedExtensionsList.Contains(extension);
    }
    
    private bool ValidateFileMetadata(FileMetadata metadata)
    {
        if (metadata == null)
            return false;
            
        if (string.IsNullOrWhiteSpace(metadata.ContentType))
            return false;
            
        // Validate content type format
        if (!IsValidContentType(metadata.ContentType))
            return false;
            
        return true;
    }
    
    public Task<ValidationResult> ValidateFileForStorageAsync(FileEntity file)
    {
        var result = new ValidationResult { IsValid = true };
        
        // Validate file name
        if (!ValidateFileName(file.Name))
        {
            result.IsValid = false;
            result.Errors.Add($"Invalid file name: {file.Name}");
        }
        
        // Validate file size
        if (!ValidateFileSize(file.Size))
        {
            result.IsValid = false;
            result.Errors.Add($"{DomainConstants.ERROR_FILE_TOO_LARGE}: {file.Size} bytes is out of allowed range ({DomainConstants.MIN_FILE_SIZE} - {DomainConstants.MAX_FILE_SIZE} bytes)");
        }
        
        // Validate file type
        if (!ValidateFileType(file.Name, DomainConstants.ALLOWED_FILE_EXTENSIONS))
        {
            result.Warnings.Add($"File type {Path.GetExtension(file.Name)} may not be supported");
        }
        
        // Validate metadata
        if (!ValidateFileMetadata(file.Metadata))
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
