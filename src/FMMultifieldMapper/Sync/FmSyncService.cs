
namespace FMMultiFieldMapper.Sync;

/// <summary>
/// Synchronize IFmObject to IFmDbObject based on FileMakerRecordId, modification date and synchronization date
/// </summary>
/// <typeparam name="TFmEntity"></typeparam>
/// <typeparam name="TDbEntity"></typeparam>
/// <typeparam name="TFmSyncEntity"></typeparam>
public abstract class FmSyncService<TFmEntity, TDbEntity, TFmSyncEntity>
    where TFmEntity : class, IFmObject, new()
    where TDbEntity : class, IFmDbObject, new()
    where TFmSyncEntity : class, IFmSync, new()
{
    /// <summary>
    /// SyncResult
    /// </summary>
    public SyncResult SyncResult { get; private set; } = new();

    /// <summary>
    /// Synchronize TFmEntity to TDbEntity
    /// </summary>
    public async Task<SyncResult> Sync(CancellationToken token = default)
    {
        await PrepareSync(token).ConfigureAwait(false);

        var fmSyncs = await GetFmSyncs<TFmSyncEntity>(token).ConfigureAwait(false);
        var dbSyncsDict = (await GetDbSyncs<TDbEntity>(token).ConfigureAwait(false))
            .ToDictionary(k => k.FileMakerRecordId, v => v.SyncTime);

        SyncResult = new();

        foreach (var fmSync in fmSyncs)
        {
            if (dbSyncsDict.TryGetValue(fmSync.FileMakerRecordId, out DateTime dbSyncTime))
            {
                if (fmSync.ModificationDate > dbSyncTime)
                {
                    if (await UpdateEntity(fmSync.FileMakerRecordId, token).ConfigureAwait(false))
                    {
                        SyncResult.Updated++;
                    }
                    else
                    {
                        SyncResult.Errors++;
                    }
                }
                else
                {
                    SyncResult.UpToDate++;
                }
            }
            else
            {
                if (await CreateEntity(fmSync.FileMakerRecordId, token).ConfigureAwait(false))
                {
                    SyncResult.Created++;
                }
                else
                {
                    SyncResult.Errors++;
                }
            }
        }

        var toDeleteIds = dbSyncsDict.Keys.Except(fmSyncs.Select(s => s.FileMakerRecordId)).ToList();
        var deleted = await DeleteEntities(toDeleteIds, token)
            .ConfigureAwait(false);

        SyncResult.Deleted = deleted;
        SyncResult.Errors += toDeleteIds.Count - deleted;

        await LinkEntities(token).ConfigureAwait(false);

        return SyncResult;
    }

    /// <summary>
    /// Get FileMaker TFmEntity modification time
    /// </summary>
    /// <returns></returns>
    public abstract Task<ICollection<FmSyncInfo>> GetFmSyncs<T>(CancellationToken token)
        where T : class, TFmSyncEntity, new();
    /// <summary>
    /// Get database TDbEntity syncronization time
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public abstract Task<ICollection<DbSyncInfo>> GetDbSyncs<T>(CancellationToken token)
        where T : class, IFmDbObject, new();


    /// <summary>
    /// UpdateEntity
    /// </summary>
    /// <returns></returns>
    public abstract Task<bool> UpdateEntity(int fileMakerRecordId, CancellationToken token);
    /// <summary>
    /// CreateEntity
    /// </summary>
    /// <returns></returns>
    protected abstract Task<bool> CreateEntity(int fileMakerRecordId, CancellationToken token);
    /// <summary>
    /// DeleteEntities
    /// </summary>
    /// <returns></returns>
    protected abstract Task<int> DeleteEntities(ICollection<int> toDeleteIds, CancellationToken token);

    /// <summary>
    /// Initialize sync if required
    /// </summary>
    /// <returns></returns>
    protected virtual Task PrepareSync(CancellationToken token)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Link to other tables if required
    /// </summary>
    /// <returns></returns>
    protected virtual Task LinkEntities(CancellationToken token)
    {
        return Task.CompletedTask;
    }
}
