using StorageFileApp.Domain.Entities.FileEntity;
using StorageFileApp.Domain.Enums;
using File = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Domain.Events;

public class FileCreatedEvent(File file) : BaseDomainEvent
{
    public File File { get; } = file ?? throw new ArgumentNullException(nameof(file));
}

public class FileStatusChangedEvent(File file, FileStatus oldStatus, FileStatus newStatus) : BaseDomainEvent
{
    public File File { get; } = file ?? throw new ArgumentNullException(nameof(file));
    public FileStatus OldStatus { get; } = oldStatus;
    public FileStatus NewStatus { get; } = newStatus;
}

public class FileDeletedEvent(Guid fileId, string fileName) : BaseDomainEvent
{
    public Guid FileId { get; } = fileId;
    public string FileName { get; } = fileName ?? throw new ArgumentNullException(nameof(fileName));
}
