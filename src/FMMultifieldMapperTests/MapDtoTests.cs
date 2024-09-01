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
        CacheFmMultiFieldMapper.MapToDtoDictionary(fmTargetTestClassWithIncludes.FmTargetTestClassMultifields,
            testDto.FmTargetTestClassMultifields);

        Assert.AreEqual(dto.FmTargetTestClassMultifields.Count, testDto.FmTargetTestClassMultifields.Count);

        foreach (var key in dto.FmTargetTestClassMultifields.Keys)
        {
            Assert.IsTrue(testDto.FmTargetTestClassMultifields.ContainsKey(key));
            CollectionAssert.AreEqual(dto.FmTargetTestClassMultifields[key], testDto.FmTargetTestClassMultifields[key]);
        }
    }
}