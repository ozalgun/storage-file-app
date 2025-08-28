namespace StorageFileApp.SharedKernel.Exceptions;

public class InvalidFileOperationException : DomainException
{
    public string Operation { get; }
    
    public InvalidFileOperationException(string operation, string reason) 
        : base($"Invalid file operation '{operation}': {reason}")
    {
        Operation = operation;
    }
}
