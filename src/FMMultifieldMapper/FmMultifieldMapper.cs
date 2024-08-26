namespace FMMultifieldMapper;

/// <summary>
/// FmMultifieldMapper
/// </summary>
public abstract class FmMultifieldMapper
{
    /// <summary>
    /// GetOrCreateMultifieldId
    /// </summary>
    /// <param name="name">Multifield name</param>
    /// <returns></returns>
    public abstract Task<int> GetOrCreateMultifieldId(string name);
    /// <summary>
    /// GetOrCreateMultifieldValueId
    /// </summary>
    /// <param name="multifieldId">Linked MultifieldId</param>
    /// <param name="value">MultifieldValue value</param>
    /// <returns></returns>
    public abstract Task<int> GetOrCreateMultifieldValueId(int multifieldId, string value);


    /// <summary>
    /// Map FmMultifield to Target collection
    /// </summary>
    /// <typeparam name="T">The type of the target multifield, implementing IFmTargetMultifield</typeparam>
    /// <param name="fmSource">FileMaker source with FmMultifield attributes</param>
    /// <param name="targetCollection">IFmTargetMultifield collection</param>
    public async Task Map<T>(object fmSource, ICollection<T> targetCollection) where T : IFmTargetMultifield, new()
    {
        ArgumentNullException.ThrowIfNull(fmSource);
        ArgumentNullException.ThrowIfNull(targetCollection);

        var targetMultifields = GetMultifieldDtos(fmSource);
        var existingEntries = targetCollection.ToList();

        foreach (var multifieldGroup in targetMultifields.GroupBy(g => g.Name))
        {
            var multifieldName = multifieldGroup.First().Name;
            var multifieldId = await GetOrCreateMultifieldId(multifieldName)
                .ConfigureAwait(false);

            foreach (var targetMultifield in multifieldGroup)
            {
                if (targetMultifield is null || targetMultifield.Value is null)
                {
                    continue;
                }

                var multifieldValue = targetMultifield.Value;
                var multifieldValueId = await GetOrCreateMultifieldValueId(multifieldId, multifieldValue)
                        .ConfigureAwait(false);

                var existingMultifield = targetCollection
                    .FirstOrDefault(m => m.FmMultiField?.Name == targetMultifield.Name
                        && m.FmMultiFieldValue?.Value == targetMultifield.Value);

                if (existingMultifield is not null)
                {
                    ArgumentNullException.ThrowIfNull(existingMultifield.FmMultiField);
                    ArgumentNullException.ThrowIfNull(existingMultifield.FmMultiFieldValue);
                    existingMultifield.FmMultiFieldValue.Order = targetMultifield.Order;
                    existingEntries.Remove(existingMultifield);
                }
                else
                {
                    T fmTargetMultifield = new()
                    {
                        FmMultifieldId = multifieldId,
                        FmMultifieldValueId = multifieldValueId
                    };
                    targetCollection.Add(fmTargetMultifield);
                }
            }

        }

        foreach (var entry in existingEntries)
        {
            targetCollection.Remove(entry);
        }
    }

    private static List<MultifieldDto> GetMultifieldDtos(object fmSource)
    {
        List<MultifieldDto> dtos = [];
        var sourceProperties = fmSource.GetType().GetProperties();
        foreach (var prop in sourceProperties)
        {
            if (prop.GetCustomAttributes(typeof(FileMakerMultifieldAttribute), false)
                                 .FirstOrDefault() is FileMakerMultifieldAttribute attribute)
            {
                dtos.Add(new(attribute.MultifieldName, prop.GetValue(fmSource)?.ToString(), attribute.Order));
            }
        }
        return dtos;
    }
}
