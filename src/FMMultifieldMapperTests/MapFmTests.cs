
using Microsoft.EntityFrameworkCore;

namespace FMMultifieldMapperTests;

[TestClass]
public class MapFmTests
{

    [TestMethod]
    public void MapToFmTest()
    {
        FmTargetTestClassDto dto = new()
        {
            FmTargetTestClassMultifields = new()
            {
                { "Themen", ["Test1", "Test2", "Test3"] },
                { "Was", ["WTest1", "WTest2", "WTest3"] },
            }
        };

        FmSourceTestClass fmTarget = new();

        CacheFmMultiFieldMapper.MapToFmObject(dto.FmTargetTestClassMultifields, fmTarget);

        Assert.AreEqual(dto.FmTargetTestClassMultifields["Themen"][0], fmTarget.Themen1);
        Assert.AreEqual(dto.FmTargetTestClassMultifields["Themen"][1], fmTarget.Themen2);
        Assert.AreEqual(dto.FmTargetTestClassMultifields["Themen"][2], fmTarget.Themen3);
    }

    [TestMethod]
    public void MapToFmEmptyTest()
    {
        FmTargetTestClassDto dto = new()
        {
            FmTargetTestClassMultifields = new()
            {
                { "Themen", ["Test1", "Test2"] }
            }
        };

        FmSourceTestClass fmTarget = new();

        CacheFmMultiFieldMapper.MapToFmObject(dto.FmTargetTestClassMultifields, fmTarget);

        Assert.AreEqual(dto.FmTargetTestClassMultifields["Themen"][0], fmTarget.Themen1);
        Assert.AreEqual(dto.FmTargetTestClassMultifields["Themen"][1], fmTarget.Themen2);
        Assert.AreEqual(string.Empty, fmTarget.Themen3);
    }
}
