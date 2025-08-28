namespace StorageFileApp.SharedKernel.Exceptions;

public class FileNotFoundException(Guid fileId) : DomainException($"File with ID {fileId} was not found.")
{
    public Guid FileId { get; } = fileId;
}
