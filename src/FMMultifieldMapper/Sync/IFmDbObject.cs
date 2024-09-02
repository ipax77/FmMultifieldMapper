namespace FMMultiFieldMapper.Sync;

/// <summary>
/// Relational db target object
/// </summary>
public interface IFmDbObject
{
    /// <summary>
    /// FileMakerRecordId
    /// </summary>
    public int FileMakerRecordId { get; set; }
    /// <summary>
    /// Latest synchronisation time
    /// </summary>
    public DateTime SyncTime { get; set; }
}