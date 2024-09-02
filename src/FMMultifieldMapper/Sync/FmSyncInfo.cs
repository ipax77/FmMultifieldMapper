namespace FMMultiFieldMapper.Sync;

/// <summary>
/// FmSyncInfo
/// </summary>
public sealed record FmSyncInfo
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
