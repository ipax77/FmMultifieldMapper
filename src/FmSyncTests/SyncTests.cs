using FMData;
using FMMultifieldMapperTests;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace FmSyncTests;

[TestClass]
public class SyncTests
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
        _dbContext.Dispose(); // Cleanup after each test
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
                ModificationDate = "01/01/2020"
            });

        TestSyncService syncService = new(_dbContext, mockFmClient.Object);

        await syncService.Sync();

        var dbEntity = _dbContext.FmTargetTestClasses.FirstOrDefault(f => f.FileMakerRecordId == 1);
        Assert.IsNotNull(dbEntity);
        Assert.AreEqual("Test", dbEntity.Name);
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
                ModificationDate = "02/01/2020"
            });

        TestSyncService syncService = new(_dbContext, mockFmClient.Object);

        // Act
        await syncService.Sync();

        // Assert
        var dbEntity = _dbContext.FmTargetTestClasses.FirstOrDefault(f => f.FileMakerRecordId == 1);
        Assert.IsNotNull(dbEntity);
        Assert.AreEqual("UpdatedName", dbEntity.Name); // Verify that the name was updated
        Assert.AreEqual(new DateTime(2020, 2, 1), dbEntity.ModificationTime); // Verify that the SyncTime was updated
    }

    [TestMethod]
    public async Task SyncTest_Delete()
    {
        // Arrange
        var existingEntity = new FmTargetTestClass
        {
            FileMakerRecordId = 1,
            Name = "ToBeDeleted",
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

        TestSyncService syncService = new(_dbContext, mockFmClient.Object);

        // Act
        await syncService.Sync();

        // Assert
        var dbEntity = _dbContext.FmTargetTestClasses.FirstOrDefault(f => f.FileMakerRecordId == 1);
        Assert.IsNull(dbEntity); // Verify that the entity was deleted
    }

    [TestMethod]
    public async Task SyncTest_NoOp()
    {
        // Arrange
        var existingEntity = new FmTargetTestClass
        {
            FileMakerRecordId = 1,
            Name = "UpToDate",
            SyncTime = new DateTime(2020, 2, 1)
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
                    ModificationDate = "01/01/2020" // Older than SyncTime
                }
                });

        TestSyncService syncService = new(_dbContext, mockFmClient.Object);

        // Act
        await syncService.Sync();

        // Assert
        var dbEntity = _dbContext.FmTargetTestClasses.FirstOrDefault(f => f.FileMakerRecordId == 1);
        Assert.IsNotNull(dbEntity);
        Assert.AreEqual("UpToDate", dbEntity.Name); // Verify that the name wasn't changed
        Assert.AreEqual(new DateTime(2020, 2, 1), dbEntity.SyncTime); // Verify that the SyncTime wasn't changed
    }

}