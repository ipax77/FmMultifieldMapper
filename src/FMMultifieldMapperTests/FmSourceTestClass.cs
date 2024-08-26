using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using FMMultifieldMapper;

namespace FMMultifieldMapperTests;

[DataContract(Name = "TestLayout")]
public class FmSourceTestClass
{
    [NotMapped]
    public int FileMakerRecordId { get; set; }
    [DataMember(Name = "Themen(1)")]
    [FileMakerMultifield(MultifieldName = "Themen", Order = 0)]
    public string? Themen1 { get; set; }
    [DataMember(Name = "Themen(2)")]
    [FileMakerMultifield(MultifieldName = "Themen", Order = 1)]
    public string? Themen2 { get; set; }
    [DataMember(Name = "Themen(3)")]
    [FileMakerMultifield(MultifieldName = "Themen", Order = 2)]
    public string? Themen3 { get; set; }
}
