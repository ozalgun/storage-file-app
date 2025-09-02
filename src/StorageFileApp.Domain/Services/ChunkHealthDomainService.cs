using StorageFileApp.Domain.Entities.ChunkEntity;
using StorageFileApp.Domain.Entities.StorageProviderEntity;
using StorageFileApp.Domain.Enums;
using StorageFileApp.SharedKernel.Exceptions;

namespace StorageFileApp.Domain.Services;

public class ChunkHealthDomainService : IChunkHealthDomainService
{
    private const int MIN_REPLICATION_COUNT = 2; // Minimum replication sayısı
    private const int MAX_REPLICATION_COUNT = 5; // Maximum replication sayısı
    
    public Task<bool> ValidateChunkHealthAsync(FileChunk chunk)
    {
        if (chunk == null)
            return Task.FromResult(false);
            
        // Basic health checks
        if (chunk.Size <= 0)
            return Task.FromResult(false);
            
        if (string.IsNullOrWhiteSpace(chunk.Checksum))
            return Task.FromResult(false);
            
        if (chunk.Status == ChunkStatus.Error || chunk.Status == ChunkStatus.Deleted)
            return Task.FromResult(false);
            
        // Check if chunk is in a valid state
        var healthStatus = GetChunkHealthStatusAsync(chunk).Result;
        return Task.FromResult(healthStatus == ChunkHealthStatus.Healthy || healthStatus == ChunkHealthStatus.Degraded);
    }
    
    public Task<IEnumerable<FileChunk>> GetUnhealthyChunksAsync(IEnumerable<FileChunk> chunks)
    {
        var unhealthyChunks = new List<FileChunk>();
        
        foreach (var chunk in chunks)
        {
            if (!ValidateChunkHealthAsync(chunk).Result)
            {
                unhealthyChunks.Add(chunk);
            }
        }
        
        return Task.FromResult<IEnumerable<FileChunk>>(unhealthyChunks);
    }
    
    public Task<bool> IsChunkReplicationNeededAsync(FileChunk chunk)
    {
        if (chunk == null)
            return Task.FromResult(false);
            
        // Check if chunk is in a state that needs replication
        var healthStatus = GetChunkHealthStatusAsync(chunk).Result;
        
        var needsReplication = healthStatus switch
        {
            ChunkHealthStatus.Corrupted => true,
            ChunkHealthStatus.Missing => true,
            ChunkHealthStatus.Degraded => true,
            _ => false
        };
        
        return Task.FromResult(needsReplication);
    }
    
    public Task<ChunkHealthStatus> GetChunkHealthStatusAsync(FileChunk chunk)
    {
        if (chunk == null)
            return Task.FromResult(ChunkHealthStatus.Unknown);
            
        // Determine health status based on chunk properties
        if (chunk.Status == ChunkStatus.Deleted)
            return Task.FromResult(ChunkHealthStatus.Missing);
            
        if (chunk.Status == ChunkStatus.Error)
            return Task.FromResult(ChunkHealthStatus.Corrupted);
            
        if (chunk.Status == ChunkStatus.Storing)
            return Task.FromResult(ChunkHealthStatus.Unknown);
            
        if (chunk.Status == ChunkStatus.Stored)
        {
            // Additional health checks for stored chunks
            if (chunk.Size <= 0)
                return Task.FromResult(ChunkHealthStatus.Corrupted);
                
            if (string.IsNullOrWhiteSpace(chunk.Checksum))
                return Task.FromResult(ChunkHealthStatus.Degraded);
                
            return Task.FromResult(ChunkHealthStatus.Healthy);
        }
        
        return Task.FromResult(ChunkHealthStatus.Unknown);
    }
    
    public Task<bool> ShouldReplicateChunkAsync(FileChunk chunk, IEnumerable<StorageProvider> availableProviders)
    {
        if (chunk == null)
            return Task.FromResult(false);
            
        var activeProviders = availableProviders.Where(p => p.IsActive).ToList();
        
        if (activeProviders.Count < MIN_REPLICATION_COUNT)
            return Task.FromResult(false); // Not enough providers for replication
            
        // Check if chunk needs replication
        if (!IsChunkReplicationNeededAsync(chunk).Result)
            return Task.FromResult(false);
            
        // Check if we have suitable replication targets
        var replicationTargets = GetReplicationTargetsAsync(chunk, activeProviders).Result;
        return Task.FromResult(replicationTargets.Any());
    }
    
    public Task<IEnumerable<StorageProvider>> GetReplicationTargetsAsync(FileChunk chunk, IEnumerable<StorageProvider> providers)
    {
        if (chunk == null)
            return Task.FromResult(Enumerable.Empty<StorageProvider>());
            
        var activeProviders = providers.Where(p => p.IsActive).ToList();
        
        // Filter out the current storage provider
        var availableTargets = activeProviders
            .Where(p => p.Id != chunk.StorageProviderId)
            .ToList();
            
        // Prioritize by provider type for replication
        var prioritizedTargets = availableTargets
            .OrderBy(p => p.Type switch
            {
                StorageProviderType.FileSystem => 1,      // Local - good for replication
                StorageProviderType.NetworkStorage => 2,   // Network - good for replication
                StorageProviderType.Database => 3,         // Database - medium for replication
                StorageProviderType.CloudStorage => 4,     // Cloud - good for backup
                _ => 5
            })
            .Take(MAX_REPLICATION_COUNT) // Limit replication targets
            .ToList();
            
        return Task.FromResult<IEnumerable<StorageProvider>>(prioritizedTargets);
    }
    
    public Task<bool> IsChunkRedundantAsync(FileChunk chunk, IEnumerable<FileChunk> allChunks)
    {
        if (chunk == null)
            return Task.FromResult(false);
            
        // Check if this chunk has redundant copies
        var redundantChunks = allChunks
            .Where(c => c.FileId == chunk.FileId && 
                       c.Order == chunk.Order && 
                       c.Id != chunk.Id &&
                       c.Status == ChunkStatus.Stored)
            .ToList();
            
        return Task.FromResult(redundantChunks.Count >= MIN_REPLICATION_COUNT - 1);
    }
    
    public Task<IEnumerable<FileChunk>> GetChunksNeedingReplicationAsync(IEnumerable<FileChunk> chunks)
    {
        var chunksNeedingReplication = new List<FileChunk>();
        
        foreach (var chunk in chunks)
        {
            if (IsChunkReplicationNeededAsync(chunk).Result)
            {
                chunksNeedingReplication.Add(chunk);
            }
        }
        
        return Task.FromResult<IEnumerable<FileChunk>>(chunksNeedingReplication);
    }
    
    public Task<ChunkReplicationPlan> CreateReplicationPlanAsync(FileChunk chunk, IEnumerable<StorageProvider> availableProviders)
    {
        if (chunk == null)
            throw new ArgumentNullException(nameof(chunk));
            
        var replicationTargets = GetReplicationTargetsAsync(chunk, availableProviders).Result;
        
        var plan = new ChunkReplicationPlan
        {
            ChunkId = chunk.Id,
            SourceProviderId = chunk.StorageProviderId,
            TargetProviderIds = replicationTargets.Select(p => p.Id).ToList(),
            Priority = DetermineReplicationPriorityAsync(chunk).Result,
            EstimatedDuration = TimeSpan.FromMinutes(5) // Default estimate
        };
        
        return Task.FromResult(plan);
    }
    
    private Task<ReplicationPriority> DetermineReplicationPriorityAsync(FileChunk chunk)
    {
        var healthStatus = GetChunkHealthStatusAsync(chunk).Result;
        
        var priority = healthStatus switch
        {
            ChunkHealthStatus.Corrupted => ReplicationPriority.Critical,
            ChunkHealthStatus.Missing => ReplicationPriority.High,
            ChunkHealthStatus.Degraded => ReplicationPriority.Medium,
            _ => ReplicationPriority.Low
        };
        
        return Task.FromResult(priority);
    }
}

public class ChunkReplicationPlan
{
    public Guid ChunkId { get; set; }
    public Guid SourceProviderId { get; set; }
    public List<Guid> TargetProviderIds { get; set; } = [];
    public ReplicationPriority Priority { get; set; }
    public TimeSpan EstimatedDuration { get; set; }
}

public enum ReplicationPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}
