
using System.ComponentModel.DataAnnotations.Schema;

namespace FMMultiFieldMapper.Sync;

/// <summary>
/// FileMaker Sync
/// </summary>
public interface IFmSync
{
    /// <summary>
    /// FileMakerRecordId
    /// </summary>
    [NotMapped]
    public int FileMakerRecordId { get; set; }
    /// <summary>
    /// ModificationDate - use the DataMember Attribute for the actual name
    /// </summary>
    public string? ModificationDate { get; set; }
}
