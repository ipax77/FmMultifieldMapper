using FMData;
using FMMultiFieldMapper;
using FMMultifieldMapperTests;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FmSyncTests;

[TestClass]
public class SyncSpecialMultiFieldTests
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

        mockFmClient.Setup(client => client.FindAsync<FmSpecialSourceTestClassSync>(
        It.IsAny<FmSpecialSourceTestClassSync>(),
        It.IsAny<int>(),
        It.IsAny<int>(),
        It.IsAny<Func<FmSpecialSourceTestClassSync, int, object>>()))
            .ReturnsAsync(new List<FmSpecialSourceTestClassSync>
            {
                new FmSpecialSourceTestClassSync
                {
                    FileMakerRecordId = 1,
                    ModificationDate = "01/01/2020"
                }
            });

        mockFmClient.Setup(client => client.GetByFileMakerIdAsync<FmSpecialSourceTestClass>(
                It.IsAny<int>(),
                It.IsAny<Func<FmSpecialSourceTestClass, int, object>>()))
            .ReturnsAsync(new FmSpecialSourceTestClass
            {
                FileMakerRecordId = 1,
                Name = "Test",
                Themen = "Test1" + Environment.NewLine + "Test2" + Environment.NewLine + "Test3" + Environment.NewLine,
                ModificationDate = "01/01/2020"
            });

        var multiFieldMapper = new InMemoryFmMultiFieldMapper(_dbContext);
        TestSpecialMultiFieldSyncService syncService = new(_dbContext, mockFmClient.Object, multiFieldMapper);

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

        mockFmClient.Setup(client => client.FindAsync<FmSpecialSourceTestClassSync>(
            It.IsAny<FmSpecialSourceTestClassSync>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<Func<FmSpecialSourceTestClassSync, int, object>>()))
                .ReturnsAsync(new List<FmSpecialSourceTestClassSync>
                {
                new FmSpecialSourceTestClassSync
                {
                    FileMakerRecordId = 1,
                    ModificationDate = "02/01/2020"
                }
                });

        mockFmClient.Setup(client => client.GetByFileMakerIdAsync<FmSpecialSourceTestClass>(
            It.IsAny<int>(),
            It.IsAny<Func<FmSpecialSourceTestClass, int, object>>()))
            .ReturnsAsync(new FmSpecialSourceTestClass
            {
                FileMakerRecordId = 1,
                Name = "UpdatedName",
                Themen = "Test1" + Environment.NewLine + "Test2" + Environment.NewLine + "Test3" + Environment.NewLine,
                ModificationDate = "02/01/2020"
            });

        var multiFieldMapper = new InMemoryFmMultiFieldMapper(_dbContext);
        TestSpecialMultiFieldSyncService syncService = new(_dbContext, mockFmClient.Object, multiFieldMapper);

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

        mockFmClient.Setup(client => client.FindAsync<FmSpecialSourceTestClassSync>(
            It.IsAny<FmSpecialSourceTestClassSync>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<Func<FmSpecialSourceTestClassSync, int, object>>()))
                .ReturnsAsync(new List<FmSpecialSourceTestClassSync>
                {
                new FmSpecialSourceTestClassSync
                {
                    FileMakerRecordId = 1,
                    ModificationDate = "02/01/2020"
                }
                });

        var fmSourceTestClass = new FmSpecialSourceTestClass
        {
            FileMakerRecordId = 1,
            Name = "UpdatedName",
            Themen = "Test1" + Environment.NewLine + "Test2" + Environment.NewLine + "Test3" + Environment.NewLine,
            ModificationDate = "02/01/2020"
        };

        mockFmClient.Setup(client => client.GetByFileMakerIdAsync<FmSpecialSourceTestClass>(
            It.IsAny<int>(),
            It.IsAny<Func<FmSpecialSourceTestClass, int, object>>()))
            .ReturnsAsync(fmSourceTestClass);

        var multiFieldMapper = new InMemoryFmMultiFieldMapper(_dbContext);
        TestSpecialMultiFieldSyncService syncService = new(_dbContext, mockFmClient.Object, multiFieldMapper);

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

        mockFmClient.Setup(client => client.FindAsync<FmSpecialSourceTestClassSync>(
            It.IsAny<FmSpecialSourceTestClassSync>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<Func<FmSpecialSourceTestClassSync, int, object>>()))
                .ReturnsAsync(new List<FmSpecialSourceTestClassSync>());

        var multiFieldMapper = new InMemoryFmMultiFieldMapper(_dbContext);
        TestSpecialMultiFieldSyncService syncService = new(_dbContext, mockFmClient.Object, multiFieldMapper);

        // Act
        await syncService.Sync();

        // Assert
        var dbEntity = _dbContext.FmTargetTestClasses.FirstOrDefault(f => f.FileMakerRecordId == 1);
        Assert.IsNull(dbEntity);
        Assert.AreEqual(0, _dbContext.FmTargetTestClassMultifields.Count());
    }
}
