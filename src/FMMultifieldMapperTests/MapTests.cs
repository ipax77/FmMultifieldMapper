using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using FMMultifieldMapper;

namespace FMMultifieldMapperTests;

[TestClass]
public class MapTests
{
    [TestMethod]
    public void SimpleMapTest()
    {
        var source = new FmSourceTestClass()
        {
            Themen1 = "Test1",
            Themen2 = "Test2",
            Themen3 = "Test3"
        };
        var target = new FmTargetTestClass();

        FmMapper.Map(source, target.FmTargetTestClassMultifields);

        var targetMultifields = target.FmTargetTestClassMultifields
            .Where(f => f.FmMultiField?.Name == "Themen")
            .ToList();
        Assert.AreEqual(3, targetMultifields.Count);
    }

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

    public class FmTargetTestClass
    {
        public int Id { get; set; }
        public ICollection<FmTargetTestClassMultifield> FmTargetTestClassMultifields { get; set; } = [];
    }

    public class FmTargetTestClassMultifield : IFmTargetMultifield
    {
        public int FmTargetTestClassMultifieldId { get; set; }
        public int FmMultifieldId { get; set; }
        public FmMultifield? FmMultiField { get; set; }
        public int FmMultifieldValueId { get; set; }
        public FmMultifieldValue? FmMultiFieldValue { get; set; }
        public int FmTargetTestClassId { get; set; }
        public FmTargetTestClass? FmTargetTestClass { get; set; }
    }
}