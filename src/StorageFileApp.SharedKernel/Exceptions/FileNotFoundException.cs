namespace StorageFileApp.SharedKernel.Exceptions;

public class FileNotFoundException : DomainException
{
    public Guid FileId { get; }
    
    public FileNotFoundException(Guid fileId) 
        : base($"File with ID {fileId} was not found.")
    {
        FileId = fileId;
    }
}
