namespace FMMultiFieldMapper.Sync;

/// <summary>
/// DbSyncInfo
/// </summary>
public sealed record DbSyncInfo
{
    /// <summary>
    /// FileMakerRecordId
    /// </summary>
    public int FileMakerRecordId { get; set; }
    /// <summary>
    /// SyncTime
    /// </summary>
    public DateTime SyncTime { get; set; }

}