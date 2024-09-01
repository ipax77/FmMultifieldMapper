namespace FMMultiFieldMapper.Sync;

/// <summary>
/// SyncInfo
/// </summary>
public sealed record SyncInfo
{
    /// <summary>
    /// FileMakerRecordId
    /// </summary>
    public int FileMakerRecordId { get; set; }
    /// <summary>
    /// ModificationDate
    /// </summary>
    public DateTime ModificationDate { get; set; }
}