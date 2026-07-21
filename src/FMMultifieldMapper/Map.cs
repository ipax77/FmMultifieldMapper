namespace FMMultiFieldMapper;

/// <summary>
/// FmMapper
/// </summary>
public static class FmMapper
{
    /// <summary>
    /// Map FmMultiField to Target collection
    /// </summary>
    /// <typeparam name="T">The type of the target multifield, implementing IFmTargetMultiField</typeparam>
    /// <param name="fmSource">FileMaker source with FmMultiField attributes</param>
    /// <param name="targetCollection">IFmTargetMultiField collection</param>
    public static void Map<T>(object fmSource, ICollection<T> targetCollection) where T : IFmTargetMultiField, new()
    {
        ArgumentNullException.ThrowIfNull(fmSource);
        ArgumentNullException.ThrowIfNull(targetCollection);

        var multifields = FmMultiFieldMap.GetMultiFieldDtos(fmSource);
        HashSet<T> existingEntries = [.. targetCollection];
        var existingByKey = new Dictionary<MultiFieldKey, T>();
        foreach (var entry in targetCollection)
        {
            if (entry.FmMultiField is not null && entry.FmMultiFieldValue is not null)
            {
                existingByKey.TryAdd(
                    new MultiFieldKey(entry.FmMultiField.Name, entry.FmMultiFieldValue.Value),
                    entry);
            }
        }

        foreach (var multifield in multifields)
        {
            if (multifield.Value is null)
            {
                continue;
            }
            var key = new MultiFieldKey(multifield.Name, multifield.Value);

            if (existingByKey.TryGetValue(key, out var existingMultiField))
            {
                existingMultiField.Order = multifield.Order;
                existingEntries.Remove(existingMultiField);
            }
            else
            {
                T fmTargetMultiField = new()
                {
                    FmMultiField = new() { Name = multifield.Name },
                    FmMultiFieldValue = new() { Value = multifield.Value }
                };

                targetCollection.Add(fmTargetMultiField);
                existingByKey.TryAdd(key, fmTargetMultiField);
            }
        }
        foreach (var entry in existingEntries)
        {
            targetCollection.Remove(entry);
        }
    }
}

internal sealed record MultiFieldDto(string Name, string? Value, int Order);
