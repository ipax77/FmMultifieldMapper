using FMMultiFieldMapper;
using Microsoft.EntityFrameworkCore;

namespace FMMultifieldMapperTests;

[TestClass]
public class MapStoTests
{
    private DbTestContext _dbContext = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<DbTestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new DbTestContext(options);
        SeedDb(_dbContext);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _dbContext.Dispose(); // Cleanup after each test
    }

    private static void SeedDb(DbTestContext context)
    {
        context.Multifields.Add(new()
        {
            Name = "Themen",
            Values = new List<FmMultiFieldValue>()
            {
                new() { Value = "Test1" },
                new() { Value = "Test2" },
                new() { Value = "Test3" }
            }
        });
        context.SaveChanges();

        FmTargetTestClass target = new()
        {
            FmTargetTestClassMultifields = new List<FmTargetTestClassMultifield>()
            {
                new() {
                    FmMultiFieldId = 1,
                    FmMultiFieldValueId = 1,
                },
                new()
                {
                    FmMultiFieldId = 1,
                    FmMultiFieldValueId = 2,
                },
                new()
                {
                    FmMultiFieldId = 1,
                    FmMultiFieldValueId = 3,
                }
            }
        };
        context.Add(target);
        context.SaveChanges();
    }

    [TestMethod]
    public async Task Map_WithExistingDbEntries_Test()
    {
        FmTargetTestClassDto dto = new()
        {
            FmTargetTestClassMultifields = new()
            {
                { "Themen", ["Test1", "Test2", "Test3"] },
                { "Was", ["WTest1", "WTest2", "WTest3"] },
            }
        };

        CacheFmMultiFieldMapper mapper = new(_dbContext);

        FmTargetTestClass fmTargetTestClass = new();
        _dbContext.FmTargetTestClasses.Add(fmTargetTestClass);
        _dbContext.SaveChanges();

        await mapper.MapFromDtoDictionary(dto.FmTargetTestClassMultifields, fmTargetTestClass.FmTargetTestClassMultifields);
        _dbContext.SaveChanges();

        var fmTargetTestClassWithIncludes = _dbContext.FmTargetTestClasses
            .Include(i => i.FmTargetTestClassMultifields)
                .ThenInclude(t => t.FmMultiField)
            .Include(i => i.FmTargetTestClassMultifields)
                .ThenInclude(t => t.FmMultiFieldValue)
            .FirstOrDefault(f => f.Id == fmTargetTestClass.Id);

        Assert.IsNotNull(fmTargetTestClassWithIncludes);
        Assert.AreEqual(6, fmTargetTestClassWithIncludes.FmTargetTestClassMultifields.Count);

        FmTargetTestClassDto testDto = new();
        testDto.FmTargetTestClassMultifields = FmMultiFieldMap
            .GetDtoDictionary(fmTargetTestClassWithIncludes.FmTargetTestClassMultifields);


        Assert.AreEqual(dto.FmTargetTestClassMultifields.Count, testDto.FmTargetTestClassMultifields.Count);

        foreach (var key in dto.FmTargetTestClassMultifields.Keys)
        {
            Assert.IsTrue(testDto.FmTargetTestClassMultifields.ContainsKey(key));
            CollectionAssert.AreEqual(dto.FmTargetTestClassMultifields[key], testDto.FmTargetTestClassMultifields[key]);
        }

        foreach (var group in fmTargetTestClassWithIncludes.FmTargetTestClassMultifields.GroupBy(x => x.FmMultiField!.Name))
        {
            CollectionAssert.AreEqual(
                Enumerable.Range(0, group.Count()).ToArray(),
                group.OrderBy(x => x.Order).Select(x => x.Order).ToArray());
        }
    }

    [TestMethod]
    public async Task MapFromDtoDictionary_ReordersExistingEntries_AndUsesZeroBasedOrder()
    {
        var target = _dbContext.FmTargetTestClasses
            .Include(x => x.FmTargetTestClassMultifields)
                .ThenInclude(x => x.FmMultiField)
            .Include(x => x.FmTargetTestClassMultifields)
                .ThenInclude(x => x.FmMultiFieldValue)
            .Single();
        var mapper = new CacheFmMultiFieldMapper(_dbContext);

        await mapper.MapFromDtoDictionary(
            new Dictionary<string, List<string>> { ["Themen"] = ["Test3", "Test1"] },
            target.FmTargetTestClassMultifields);
        await _dbContext.SaveChangesAsync();

        var ordered = target.FmTargetTestClassMultifields.OrderBy(x => x.Order).ToArray();
        Assert.HasCount(2, ordered);
        Assert.AreEqual("Test3", ordered[0].FmMultiFieldValue!.Value);
        Assert.AreEqual(0, ordered[0].Order);
        Assert.AreEqual("Test1", ordered[1].FmMultiFieldValue!.Value);
        Assert.AreEqual(1, ordered[1].Order);
    }
}
