
using System.Globalization;
using FMData;
using FMMultiFieldMapper.Sync;
using FMMultifieldMapperTests;
using Microsoft.EntityFrameworkCore;

namespace FmSyncTests;

public class TestSyncService(DbTestContext context, IFileMakerApiClient fmClient)
    : FmSyncService<FmSourceTestClass, FmTargetTestClass, FmSourceTestClassSync>
{
    private readonly DbTestContext context = context;
    private readonly IFileMakerApiClient fmClient = fmClient;

    public override async Task<ICollection<DbSyncInfo>> GetDbSyncs<T>(CancellationToken token)
    {
        var syncs = from e in context.Set<T>()
                    select new DbSyncInfo()
                    {
                        FileMakerRecordId = e.FileMakerRecordId,
                        SyncTime = e.SyncTime
                    };
        return await syncs.ToListAsync(token);
    }

    public override async Task<ICollection<FmSyncInfo>> GetFmSyncs<T>(CancellationToken token)
    {
        T fmSync = new();
        List<T> fmSyncs = [];

        int i = 0;
        while (!token.IsCancellationRequested)
        {
            var results = await fmClient.FindAsync<T>(fmSync, i * 100, 100, (o, id) => o.FileMakerRecordId = id);
            fmSyncs.AddRange(results);
            int count = results.Count();
            if (count == 0 || count < 100)
            {
                break;
            }
            i++;
        }
        return fmSyncs.Select(s => new FmSyncInfo()
        {
            FileMakerRecordId = s.FileMakerRecordId,
            ModificationDate = GetDateTimeFromFmString(s.ModificationDate)
        }).ToList();
    }

    public override async Task<bool> UpdateEntity(int fileMakerRecordId, CancellationToken token)
    {
        try
        {
            var dbEntity = await context.FmTargetTestClasses
                .FirstOrDefaultAsync(f => f.FileMakerRecordId == fileMakerRecordId, token);

            ArgumentNullException.ThrowIfNull(dbEntity);

            var fmEntity = await fmClient.GetByFileMakerIdAsync<FmSourceTestClass>(fileMakerRecordId,
                (o, id) => o.FileMakerRecordId = id);

            ArgumentNullException.ThrowIfNull(fmEntity);

            dbEntity.Name = fmEntity.Name;
            dbEntity.ModificationTime = GetDateTimeFromFmString(fmEntity.ModificationDate);

            dbEntity.SyncTime = DateTime.UtcNow;
            await context.SaveChangesAsync(token);
        }
        catch (Exception ex)
        {
            SyncResult.ErrorInfos.Add($"Failed updating {fileMakerRecordId}: {ex.Message}");
            return false;
        }
        return true;
    }

    protected override async Task<bool> CreateEntity(int fileMakerRecordId, CancellationToken token)
    {
        try
        {
            var fmEntity = await fmClient.GetByFileMakerIdAsync<FmSourceTestClass>(fileMakerRecordId,
                    (o, id) => o.FileMakerRecordId = id);

            ArgumentNullException.ThrowIfNull(fmEntity);

            var dbEntity = new FmTargetTestClass()
            {
                FileMakerRecordId = fmEntity.FileMakerRecordId,
                Name = fmEntity.Name,
                ModificationTime = GetDateTimeFromFmString(fmEntity.ModificationDate),
                SyncTime = DateTime.UtcNow
            };

            context.FmTargetTestClasses.Add(dbEntity);
            await context.SaveChangesAsync(token);

            return true;
        }
        catch (Exception ex)
        {
            SyncResult.ErrorInfos.Add($"Failed creating {fileMakerRecordId}: {ex.Message}");
            return false;
        }
    }

    protected override async Task<int> DeleteEntities(ICollection<int> toDeleteIds, CancellationToken token)
    {
        if (toDeleteIds.Count == 0)
        {
            return 0;
        }

        try
        {
            if (context.Database.IsInMemory())
            {
                int i = 0;
                foreach (var entity in context.FmTargetTestClasses.Where(x => toDeleteIds.Contains(x.FileMakerRecordId)))
                {
                    context.FmTargetTestClasses.Remove(entity);
                    i++;
                }
                await context.SaveChangesAsync(token);
                return i;
            }
            else
            {
                return await context.FmTargetTestClasses
                    .Where(x => toDeleteIds.Contains(x.FileMakerRecordId))
                    .ExecuteDeleteAsync(token);
            }
        }
        catch (Exception ex)
        {
            SyncResult.ErrorInfos.Add($"Failed deleting {toDeleteIds.Count} entities: {ex.Message}");
            return 0;
        }
    }

    private DateTime GetDateTimeFromFmString(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return DateTime.MinValue;
        }

        string[] formats = ["MM/dd/yyyy", "MM/dd/yyyy HH:mm:ss"];
        var culture = new CultureInfo("en-US");
        DateTimeStyles styles = DateTimeStyles.None;

        if (DateTime.TryParseExact(source, formats, culture, styles, out DateTime dateValue))
        {
            return dateValue;
        }
        else
        {
            SyncResult.ErrorInfos.Add($"Failed parsing FM date: {source}");
        }

        return DateTime.MinValue;
    }
}