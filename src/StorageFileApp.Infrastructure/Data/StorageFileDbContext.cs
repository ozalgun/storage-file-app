using Microsoft.EntityFrameworkCore;
using StorageFileApp.Domain.Entities.FileEntity;
using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.ValueObjects;
using FileEntity = StorageFileApp.Domain.Entities.FileEntity.File;

namespace StorageFileApp.Infrastructure.Data;

public class StorageFileDbContext(DbContextOptions<StorageFileDbContext> options) : DbContext(options)
{
    public DbSet<FileEntity> Files { get; set; }
    public DbSet<FileChunk> FileChunks { get; set; }
    public DbSet<StorageProvider> StorageProviders { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // File entity configuration
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.Name).IsRequired().HasMaxLength(255);
            entity.Property(f => f.Size).IsRequired();
            entity.Property(f => f.Checksum).IsRequired().HasMaxLength(64);
            entity.Property(f => f.Status).IsRequired();
            entity.Property(f => f.CreatedAt).IsRequired();
            entity.Property(f => f.UpdatedAt);

            // FileMetadata value object configuration
            entity.OwnsOne(f => f.Metadata, metadata =>
            {
                metadata.Property(m => m.ContentType).IsRequired().HasMaxLength(100);
                metadata.Property(m => m.Description).HasMaxLength(500);
                metadata.ToTable("FileMetadata");
            });

            entity.HasIndex(f => f.Name);
            entity.HasIndex(f => f.Status);
            entity.HasIndex(f => f.CreatedAt);
        });

        // FileChunk entity configuration
        modelBuilder.Entity<FileChunk>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.Property(c => c.FileId).IsRequired();
            entity.Property(c => c.Order).IsRequired();
            entity.Property(c => c.Size).IsRequired();
            entity.Property(c => c.Checksum).IsRequired().HasMaxLength(64);
            entity.Property(c => c.StorageProviderId).IsRequired();
            entity.Property(c => c.Status).IsRequired();
            entity.Property(c => c.CreatedAt).IsRequired();
            entity.Property(c => c.UpdatedAt);

            entity.HasIndex(c => c.FileId);
            entity.HasIndex(c => c.StorageProviderId);
            entity.HasIndex(c => c.Status);
            entity.HasIndex(c => new { c.FileId, c.Order }).IsUnique();

            // Foreign key relationships
            entity.HasOne<FileEntity>()
                  .WithMany()
                  .HasForeignKey(c => c.FileId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<StorageProvider>()
                  .WithMany()
                  .HasForeignKey(c => c.StorageProviderId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // StorageProvider entity configuration
        modelBuilder.Entity<StorageProvider>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.Name).IsRequired().HasMaxLength(100);
            entity.Property(p => p.Type).IsRequired();
            entity.Property(p => p.ConnectionString).IsRequired().HasMaxLength(500);
            entity.Property(p => p.IsActive).IsRequired();
            entity.Property(p => p.CreatedAt).IsRequired();
            entity.Property(p => p.UpdatedAt);

            entity.HasIndex(p => p.Name).IsUnique();
            entity.HasIndex(p => p.Type);
            entity.HasIndex(p => p.IsActive);
        });
    }
}
