using FMMultiFieldMapper;
using Microsoft.EntityFrameworkCore;

namespace FMMultifieldMapperTests;

[TestClass]
public partial class SpecialMultiFieldsTests
{
    private DbTestContext _dbContext = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DbTestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DbTestContext(options);
    }

    [TestMethod]
    public void MapFmToDtoTest()
    {
        var source = new FmSpecialSourceTestClass()
        {
            Themen = "Test1" + "\r\n" + "Test2" + "\r\n" + "Test3" + "\r\n",
        };
        var target = new FmTargetTestClass();

        FmMapper.Map(source, target.FmTargetTestClassMultifields);

        var targetMultifields = target.FmTargetTestClassMultifields
            .Where(f => f.FmMultiField?.Name == "Themen")
            .ToList();
        Assert.AreEqual(3, targetMultifields.Count);
    }

    [TestMethod]
    public void MapDtoToFMTest()
    {
        FmTargetTestClassDto dto = new()
        {
            FmTargetTestClassMultifields = new()
            {
                { "Themen", ["Test1", "Test2"] }
            }
        };

        FmSpecialSourceTestClass fmTarget = new();

        CacheFmMultiFieldMapper.MapToFmObject(dto.FmTargetTestClassMultifields, fmTarget);

        Assert.AreEqual("Test1" + "\r\n" + "Test2" + "\r\n", fmTarget.Themen);
    }

    [TestMethod]
    public async Task MapFmToDbTest()
    {
        var target = new FmTargetTestClass();
        _dbContext.FmTargetTestClasses.Add(target);
        _dbContext.SaveChanges();

        var source = new FmSpecialSourceTestClass()
        {
            Themen = "Test1" + "\r\n" + "Test2" + "\r\n" + "Test3" + "\r\n",
        };

        InMemoryFmMultiFieldMapper mapper = new(_dbContext);
        await mapper.Map(source, target.FmTargetTestClassMultifields);
        _dbContext.SaveChanges();

        Assert.AreEqual(1, _dbContext.Multifields.Count());
        Assert.AreEqual(3, _dbContext.MultifieldValues.Count());
    }
}