using FMMultiFieldMapper.Sync;

namespace FMMultifieldMapperTests;

public class FmTargetTestClass : IFmDbObject
{
    public int Id { get; set; }
    public int FileMakerRecordId { get; set; }
    public string? Name { get; set; }
    public DateTime ModificationTime { get; set; }
    public DateTime SyncTime { get; set; }
    public ICollection<FmTargetTestClassMultifield> FmTargetTestClassMultifields { get; set; } = [];
}
