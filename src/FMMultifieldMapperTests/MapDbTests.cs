using FMMultiFieldMapper;
using Microsoft.EntityFrameworkCore;

namespace FMMultifieldMapperTests;

[TestClass]
public class MapDbTests
{
    private TestContext _dbContext = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new TestContext(options);
        SeedDb(_dbContext);
    }

    [TestCleanup]
    public void TestCleanup()
    {
        _dbContext.Dispose(); // Cleanup after each test
    }

    private static void SeedDb(TestContext context)
    {
        context.Multifields.Add(new()
        {
            Name = "Themen",
            Values = new List<FmMultiFieldValue>()
            {
                new() { Value = "Test1", Order = 1 },
                new() { Value = "Test2", Order = 2 },
                new() { Value = "Test3", Order = 3 }
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
        var target = _dbContext.FmTargetTestClasses.FirstOrDefault();
        Assert.IsNotNull(target);
        Assert.AreEqual(1, _dbContext.Multifields.Count());
        Assert.AreEqual(3, _dbContext.MultifieldValues.Count());

        var source = new FmSourceTestClass()
        {
            Themen1 = "Test3",
            Themen2 = "Test4",
            Themen3 = "Test5"
        };

        InMemoryFmMultiFieldMapper mapper = new(_dbContext);
        await mapper.Map(source, target.FmTargetTestClassMultifields);
        _dbContext.SaveChanges();

        Assert.AreEqual(1, _dbContext.Multifields.Count());
        Assert.AreEqual(5, _dbContext.MultifieldValues.Count());
    }

    [TestMethod]
    public async Task CacheMap_WithExistingDbEntries_Test()
    {
        var target = _dbContext.FmTargetTestClasses.FirstOrDefault();
        Assert.IsNotNull(target);
        Assert.AreEqual(1, _dbContext.Multifields.Count());
        Assert.AreEqual(3, _dbContext.MultifieldValues.Count());

        var source = new FmSourceTestClass()
        {
            Themen1 = "Test3",
            Themen2 = "Test4",
            Themen3 = "Test5"
        };

        CacheFmMultiFieldMapper mapper = new(_dbContext);
        await mapper.Map(source, target.FmTargetTestClassMultifields);
        _dbContext.SaveChanges();

        Assert.AreEqual(1, _dbContext.Multifields.Count());
        Assert.AreEqual(5, _dbContext.MultifieldValues.Count());
    }

    [TestMethod]
    public async Task Map_WithNewMultifieldsAndValues_Test()
    {
        var target = _dbContext.FmTargetTestClasses.FirstOrDefault();
        Assert.IsNotNull(target);

        // Create a source with new multifields and values
        var source = new FmSourceTestClass()
        {
            Themen1 = "NewTest1",
            Themen2 = "NewTest2",
            Themen3 = "NewTest3"
        };

        var mapper = new InMemoryFmMultiFieldMapper(_dbContext);
        await mapper.Map(source, target.FmTargetTestClassMultifields);
        _dbContext.SaveChanges();

        // Check that new multifields and values are added
        Assert.AreEqual(1, _dbContext.Multifields.Count()); // Should still be 1 if "Themen" was not created again
        Assert.AreEqual(6, _dbContext.MultifieldValues.Count()); // Should include new values
    }

    [TestMethod]
    public async Task Map_WithUpdatesToExistingValues_Test()
    {
        var target = _dbContext.FmTargetTestClasses.FirstOrDefault();
        Assert.IsNotNull(target);

        // Create a source with updated values
        var source = new FmSourceTestClass()
        {
            Themen1 = "Test1 Updated", // Existing value
            Themen2 = "Test2 Updated", // Existing value
            Themen3 = "NewTest6" // New value
        };

        var mapper = new InMemoryFmMultiFieldMapper(_dbContext);
        await mapper.Map(source, target.FmTargetTestClassMultifields);
        _dbContext.SaveChanges();

        // Verify updates
        var updatedValue1 = _dbContext.MultifieldValues.First(v => v.Value == "Test1 Updated");
        var updatedValue2 = _dbContext.MultifieldValues.First(v => v.Value == "Test2 Updated");
        var newValue6 = _dbContext.MultifieldValues.First(v => v.Value == "NewTest6");

        Assert.IsNotNull(updatedValue1);
        Assert.IsNotNull(updatedValue2);
        Assert.IsNotNull(newValue6);
    }


    [TestMethod]
    public async Task Map_WithEmptySource_NoChanges_Test()
    {
        var target = _dbContext.FmTargetTestClasses.FirstOrDefault();
        Assert.IsNotNull(target);

        // Provide an empty source
        var source = new FmSourceTestClass();

        var mapper = new InMemoryFmMultiFieldMapper(_dbContext);
        await mapper.Map(source, target.FmTargetTestClassMultifields);
        _dbContext.SaveChanges();

        // Verify no changes
        Assert.AreEqual(1, _dbContext.Multifields.Count());
        Assert.AreEqual(3, _dbContext.MultifieldValues.Count());
    }

    [TestMethod]
    public async Task Map_WithUniqueConstraints_Test()
    {
        var target = _dbContext.FmTargetTestClasses.FirstOrDefault();
        Assert.IsNotNull(target);

        // Attempt to add duplicate values
        var source = new FmSourceTestClass()
        {
            Themen1 = "Test1", // Existing value
            Themen2 = "Test1"  // Duplicate value
        };

        var mapper = new InMemoryFmMultiFieldMapper(_dbContext);
        await mapper.Map(source, target.FmTargetTestClassMultifields);
        _dbContext.SaveChanges();

        // Verify no duplicates
        var duplicateValues = _dbContext.MultifieldValues
            .Where(v => v.Value == "Test1")
            .ToList();

        Assert.AreEqual(1, duplicateValues.Count); // Ensure no duplicate entries are created
    }

}
