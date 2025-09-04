using Microsoft.EntityFrameworkCore;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.Infrastructure.Data;

namespace StorageFileApp.Infrastructure.Data;

public static class SeedData
{
    public static async Task SeedAsync(StorageFileDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Seed Storage Providers
        await SeedStorageProvidersAsync(context);

        // Save changes
        await context.SaveChangesAsync();
    }

    private static async Task SeedStorageProvidersAsync(StorageFileDbContext context)
    {
        // Check if storage providers already exist
        if (await context.StorageProviders.AnyAsync())
        {
            return; // Already seeded
        }

        var storageProviders = new List<StorageProvider>
        {
            new StorageProvider(
                name: "Local File System Storage",
                type: StorageProviderType.FileSystem,
                connectionString: "BasePath=storage/local"
            ),
            new StorageProvider(
                name: "MinIO S3 Storage",
                type: StorageProviderType.MinIO,
                connectionString: "ServiceURL=http://localhost:9000;AccessKey=minioadmin;SecretKey=minioadmin123;BucketName=storage-file-app"
            )
        };

        await context.StorageProviders.AddRangeAsync(storageProviders);
    }
}
