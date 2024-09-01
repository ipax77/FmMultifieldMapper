
namespace FMMultiFieldMapper.Sync;

/// <summary>
/// Synchronize TFmEntities to TDbEntities based on FileMakerRecordId and modification date
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
    /// Synchronize TFmEntity to TDbEntity
    /// </summary>
    public async Task<SyncResult> Sync(CancellationToken token = default)
    {
        await PrepareSync(token).ConfigureAwait(false);

        var fmSyncs = await GetFmSyncs<TFmSyncEntity>(token).ConfigureAwait(false);
        var dbSyncsDict = (await GetDbSyncs<TDbEntity>(token).ConfigureAwait(false))
            .ToDictionary(k => k.FileMakerRecordId, v => v.ModificationDate);

        SyncResult result = new();

        foreach (var fmSync in fmSyncs)
        {
            if (dbSyncsDict.TryGetValue(fmSync.FileMakerRecordId, out DateTime dbSyncTime))
            {
                if (fmSync.ModificationDate > dbSyncTime)
                {
                    if (await UpdateEntity(fmSync.FileMakerRecordId, token).ConfigureAwait(false))
                    {
                        result.Updated++;
                    }
                    else
                    {
                        result.Errors++;
                    }
                }
                else
                {
                    result.UpToDate++;
                }
            }
            else
            {
                if (await CreateEntity(fmSync.FileMakerRecordId, token).ConfigureAwait(false))
                {
                    result.Created++;
                }
                else
                {
                    result.Errors++;
                }
            }
        }

        await DeleteEntities(dbSyncsDict.Keys.Except(fmSyncs.Select(s => s.FileMakerRecordId)).ToList(), token)
            .ConfigureAwait(false);

        await LinkEntities(token).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Get FileMaker sync information for TFmEntity
    /// </summary>
    /// <returns></returns>
    public abstract Task<ICollection<SyncInfo>> GetFmSyncs<T>(CancellationToken token)
        where T : class, TFmSyncEntity, new();
    /// <summary>
    /// Get database sync information for TDbEntity
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public abstract Task<ICollection<SyncInfo>> GetDbSyncs<T>(CancellationToken token)
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
    /// Initialize job if needed
    /// </summary>
    /// <returns></returns>
    protected virtual Task PrepareSync(CancellationToken token)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Link to other tables if needed
    /// </summary>
    /// <returns></returns>
    protected virtual Task LinkEntities(CancellationToken token)
    {
        return Task.CompletedTask;
    }
}
