
using System.ComponentModel.DataAnnotations.Schema;

namespace FMMultiFieldMapper.Sync;

/// <summary>
/// FileMaker dto object
/// </summary>
public interface IFmObject
{
    /// <summary>
    /// FileMakerRecordId
    /// </summary>
    [NotMapped]
    public int FileMakerRecordId { get; set; }
}
