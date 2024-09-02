
namespace FMMultiFieldMapper.Sync;

/// <summary>
/// SyncResult
/// </summary>
public record SyncResult
{
    /// <summary>
    /// SyncTime
    /// </summary>
    public DateTime SyncTime { get; private set; } = DateTime.UtcNow;
    /// <summary>
    /// Created
    /// </summary>
    public int Created { get; set; }
    /// <summary>
    /// Updated
    /// </summary>
    public int Updated { get; set; }
    /// <summary>
    /// Deleted
    /// </summary>
    public int Deleted { get; set; }
    /// <summary>
    /// UpToDate
    /// </summary>
    public int UpToDate { get; set; }
    /// <summary>
    /// Errors
    /// </summary>
    public int Errors { get; set; }
    /// <summary>
    /// Exceptions
    /// </summary>
    public ICollection<string> ErrorInfos { get; } = [];
}