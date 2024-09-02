using FMData;
using FMMultiFieldMapper;
using FMMultifieldMapperTests;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FmSyncTests;

[TestClass]
public class SyncMultiFieldTests
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

    [TestCleanup]
    public void TestCleanup()
    {
        _dbContext.Dispose();
    }

    [TestMethod]
    public async Task SyncTest()
    {
        var mockFmClient = new Mock<IFileMakerApiClient>();

        mockFmClient.Setup(client => client.FindAsync<FmSourceTestClassSync>(
        It.IsAny<FmSourceTestClassSync>(),
        It.IsAny<int>(),
        It.IsAny<int>(),
        It.IsAny<Func<FmSourceTestClassSync, int, object>>()))
            .ReturnsAsync(new List<FmSourceTestClassSync>
            {
                new FmSourceTestClassSync
                {
                    FileMakerRecordId = 1,
                    ModificationDate = "01/01/2020"
                }
            });

        mockFmClient.Setup(client => client.GetByFileMakerIdAsync<FmSourceTestClass>(
                It.IsAny<int>(),
                It.IsAny<Func<FmSourceTestClass, int, object>>()))
            .ReturnsAsync(new FmSourceTestClass
            {
                FileMakerRecordId = 1,
                Name = "Test",
                Themen1 = "Test1",
                Themen2 = "Test2",
                Themen3 = "Test3",
                ModificationDate = "01/01/2020"
            });

        var multiFieldMapper = new InMemoryFmMultiFieldMapper(_dbContext);
        TestMultiFieldSyncService syncService = new(_dbContext, mockFmClient.Object, multiFieldMapper);

        await syncService.Sync();

        var dbEntity = _dbContext.FmTargetTestClasses
            .IncludeMultiFields()
            .FirstOrDefault(f => f.FileMakerRecordId == 1);
        Assert.IsNotNull(dbEntity);
        Assert.AreEqual("Test", dbEntity.Name);
        Assert.AreEqual(3, dbEntity.FmTargetTestClassMultifields.Count);
    }

    [TestMethod]
    public async Task SyncTest_Update()
    {
        // Arrange
        var existingEntity = new FmTargetTestClass
        {
            FileMakerRecordId = 1,
            Name = "OldName",
            SyncTime = new DateTime(2020, 1, 1)
        };
        _dbContext.FmTargetTestClasses.Add(existingEntity);
        await _dbContext.SaveChangesAsync();

        var mockFmClient = new Mock<IFileMakerApiClient>();

        mockFmClient.Setup(client => client.FindAsync<FmSourceTestClassSync>(
            It.IsAny<FmSourceTestClassSync>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<Func<FmSourceTestClassSync, int, object>>()))
                .ReturnsAsync(new List<FmSourceTestClassSync>
                {
                new FmSourceTestClassSync
                {
                    FileMakerRecordId = 1,
                    ModificationDate = "02/01/2020"
                }
                });

        mockFmClient.Setup(client => client.GetByFileMakerIdAsync<FmSourceTestClass>(
            It.IsAny<int>(),
            It.IsAny<Func<FmSourceTestClass, int, object>>()))
            .ReturnsAsync(new FmSourceTestClass
            {
                FileMakerRecordId = 1,
                Name = "UpdatedName",
                Themen1 = "Test1",
                Themen2 = "Test2",
                Themen3 = "Test3",
                ModificationDate = "02/01/2020"
            });

        var multiFieldMapper = new InMemoryFmMultiFieldMapper(_dbContext);
        TestMultiFieldSyncService syncService = new(_dbContext, mockFmClient.Object, multiFieldMapper);

        // Act
        await syncService.Sync();

        // Assert
        var dbEntity = _dbContext.FmTargetTestClasses
            .IncludeMultiFields()
            .FirstOrDefault(f => f.FileMakerRecordId == 1);
        Assert.IsNotNull(dbEntity);
        Assert.AreEqual("UpdatedName", dbEntity.Name);
        Assert.AreEqual(new DateTime(2020, 2, 1), dbEntity.ModificationTime);
        Assert.AreEqual(3, dbEntity.FmTargetTestClassMultifields.Count);
    }

    [TestMethod]
    public async Task SyncTest_UpdateMultiFields()
    {
        // Arrange
        var fmMultiFieldValue = new FmMultiFieldValue() { Value = "Test1" };
        var existingEntity = new FmTargetTestClass
        {
            FileMakerRecordId = 1,
            Name = "OldName",
            FmTargetTestClassMultifields = [
                new FmTargetTestClassMultifield()
                    {
                         FmMultiField = new() { Name = "Themen", Values = [ fmMultiFieldValue ] },
                         FmMultiFieldValue = fmMultiFieldValue,
                         Order = 0
                    }
            ],
            SyncTime = new DateTime(2020, 1, 1)
        };
        _dbContext.FmTargetTestClasses.Add(existingEntity);
        await _dbContext.SaveChangesAsync();

        var mockFmClient = new Mock<IFileMakerApiClient>();

        mockFmClient.Setup(client => client.FindAsync<FmSourceTestClassSync>(
            It.IsAny<FmSourceTestClassSync>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<Func<FmSourceTestClassSync, int, object>>()))
                .ReturnsAsync(new List<FmSourceTestClassSync>
                {
                new FmSourceTestClassSync
                {
                    FileMakerRecordId = 1,
                    ModificationDate = "02/01/2020"
                }
                });

        var fmSourceTestClass = new FmSourceTestClass
        {
            FileMakerRecordId = 1,
            Name = "UpdatedName",
            Themen1 = "Test2",
            Themen2 = "Test3",
            Themen3 = "Test4",
            ModificationDate = "02/01/2020"
        };

        mockFmClient.Setup(client => client.GetByFileMakerIdAsync<FmSourceTestClass>(
            It.IsAny<int>(),
            It.IsAny<Func<FmSourceTestClass, int, object>>()))
            .ReturnsAsync(fmSourceTestClass);

        var multiFieldMapper = new InMemoryFmMultiFieldMapper(_dbContext);
        TestMultiFieldSyncService syncService = new(_dbContext, mockFmClient.Object, multiFieldMapper);

        // Act
        await syncService.Sync();

        // Assert
        var dbEntity = _dbContext.FmTargetTestClasses
            .IncludeMultiFields()
            .FirstOrDefault(f => f.FileMakerRecordId == 1);
        Assert.IsNotNull(dbEntity);
        Assert.AreEqual("UpdatedName", dbEntity.Name);
        Assert.AreEqual(new DateTime(2020, 2, 1), dbEntity.ModificationTime);
        Assert.AreEqual(3, dbEntity.FmTargetTestClassMultifields.Count);

        var fmDto = new FmTargetTestClassDto();
        var dbDto = new FmTargetTestClassDto();
        fmDto.FmTargetTestClassMultifields = FmMultiFieldMap.GetDtoDictionary(fmSourceTestClass);
        dbDto.FmTargetTestClassMultifields = FmMultiFieldMap.GetDtoDictionary(dbEntity.FmTargetTestClassMultifields);

        Assert.AreEqual(fmDto.FmTargetTestClassMultifields.Count, dbDto.FmTargetTestClassMultifields.Count);

        foreach (var key in fmDto.FmTargetTestClassMultifields.Keys)
        {
            Assert.IsTrue(dbDto.FmTargetTestClassMultifields.ContainsKey(key));
            CollectionAssert.AreEqual(dbDto.FmTargetTestClassMultifields[key], fmDto.FmTargetTestClassMultifields[key]);
        }
    }

    [TestMethod]
    public async Task SyncTest_Delete()
    {
        // Arrange
        var fmMultiFieldValue = new FmMultiFieldValue() { Value = "Test1" };
        var existingEntity = new FmTargetTestClass
        {
            FileMakerRecordId = 1,
            Name = "ToBeDeleted",
            FmTargetTestClassMultifields = [
                new FmTargetTestClassMultifield()
                    {
                         FmMultiField = new() { Name = "Themen", Values = [ fmMultiFieldValue ] },
                         FmMultiFieldValue = fmMultiFieldValue,
                         Order = 0
                    }
            ],
            SyncTime = new DateTime(2020, 1, 1)
        };
        _dbContext.FmTargetTestClasses.Add(existingEntity);
        await _dbContext.SaveChangesAsync();

        var mockFmClient = new Mock<IFileMakerApiClient>();

        mockFmClient.Setup(client => client.FindAsync<FmSourceTestClassSync>(
            It.IsAny<FmSourceTestClassSync>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<Func<FmSourceTestClassSync, int, object>>()))
                .ReturnsAsync(new List<FmSourceTestClassSync>());

        var multiFieldMapper = new InMemoryFmMultiFieldMapper(_dbContext);
        TestMultiFieldSyncService syncService = new(_dbContext, mockFmClient.Object, multiFieldMapper);

        // Act
        await syncService.Sync();

        // Assert
        var dbEntity = _dbContext.FmTargetTestClasses.FirstOrDefault(f => f.FileMakerRecordId == 1);
        Assert.IsNull(dbEntity);
        Assert.AreEqual(0, _dbContext.FmTargetTestClassMultifields.Count());
    }
}

public static class FmTargetTestClassesExtensions
{
    public static IQueryable<FmTargetTestClass> IncludeMultiFields(this IQueryable<FmTargetTestClass> fmTargetTestClasses)
    {
        return fmTargetTestClasses
            .Include(i => i.FmTargetTestClassMultifields)
                .ThenInclude(i => i.FmMultiField)
            .Include(i => i.FmTargetTestClassMultifields)
                .ThenInclude(i => i.FmMultiFieldValue);
    }
}