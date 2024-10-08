using FMMultiFieldMapper;

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
                FmMultiField = new FmMultiField { Name = "Themen" },
                FmMultiFieldValue = new FmMultiFieldValue { Value = "Test1" }
            },
            new FmTargetTestClassMultifield
            {
                FmMultiField = new FmMultiField { Name = "Themen" },
                FmMultiFieldValue = new FmMultiFieldValue { Value = "Test3" }
            },
            new FmTargetTestClassMultifield
            {
                FmMultiField = new FmMultiField { Name = "ObsoleteThemen" },
                FmMultiFieldValue = new FmMultiFieldValue { Value = "Obsolete" }
            }
        };

        FmMapper.Map(source, targetCollection);

        var targetMultifields = targetCollection
            .Where(f => f.FmMultiField?.Name == "Themen")
            .OrderBy(f => f.Order)
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