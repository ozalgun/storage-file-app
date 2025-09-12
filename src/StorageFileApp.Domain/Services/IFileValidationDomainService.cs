using StorageFileApp.Domain.ValueObjects;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Services;

public interface IFileValidationDomainService
{
    Task<ValidationResult> ValidateFileForStorageAsync(FileEntity file);
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    
    public static ValidationResult Success() => new() { IsValid = true };
    public static ValidationResult Failure(params string[] errors) => new() { IsValid = false, Errors = errors.ToList() };
}
