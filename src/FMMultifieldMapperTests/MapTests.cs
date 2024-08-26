using FMMultifieldMapper;

namespace FMMultifieldMapperTests;

[TestClass]
public partial class MapTests
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

    [TestMethod]
    public void MapWithUpdatesAndRemovalsTest()
    {
        var source = new FmSourceTestClass()
        {
            Themen1 = "Test1",
            Themen2 = "Test2",
            Themen3 = "Test3 Updated"
        };

        var targetCollection = new List<FmTargetTestClassMultifield>
        {
            new FmTargetTestClassMultifield
            {
                FmMultiField = new FmMultifield { Name = "Themen" },
                FmMultiFieldValue = new FmMultifieldValue { Value = "Test1", Order = 0 }
            },
            new FmTargetTestClassMultifield
            {
                FmMultiField = new FmMultifield { Name = "Themen" },
                FmMultiFieldValue = new FmMultifieldValue { Value = "Test3", Order = 2 }
            },
            new FmTargetTestClassMultifield
            {
                FmMultiField = new FmMultifield { Name = "ObsoleteThemen" },
                FmMultiFieldValue = new FmMultifieldValue { Value = "Obsolete", Order = 99 }
            }
        };

        FmMapper.Map(source, targetCollection);

        var targetMultifields = targetCollection
            .Where(f => f.FmMultiField?.Name == "Themen")
            .OrderBy(f => f.FmMultiFieldValue?.Order)
            .ToList();

        Assert.AreEqual(3, targetMultifields.Count);
        Assert.AreEqual("Test1", targetMultifields[0].FmMultiFieldValue?.Value);
        Assert.AreEqual("Test2", targetMultifields[1].FmMultiFieldValue?.Value);
        Assert.AreEqual("Test3 Updated", targetMultifields[2].FmMultiFieldValue?.Value);

        var removedItem = targetCollection
            .FirstOrDefault(f => f.FmMultiField?.Name == "ObsoleteThemen");
        Assert.IsNull(removedItem);
    }
}