namespace StorageFileApp.SharedKernel.Exceptions;

public class InvalidFileOperationException(string operation, string reason)
    : DomainException($"Invalid file operation '{operation}': {reason}")
{
    public string Operation { get; } = operation;
}
