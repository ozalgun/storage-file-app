using StorageFileApp.Application.Interfaces;
using StorageFileApp.Domain.Entities.StorageProviderEntity;

namespace StorageFileApp.Application.Interfaces;

public interface IStorageProviderFactory
{
    IStorageService GetStorageService(StorageProvider provider);
    IStorageService GetDefaultStorageService();
    IEnumerable<IStorageService> GetAllStorageServices();
}
