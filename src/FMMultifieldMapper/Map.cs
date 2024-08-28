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

        var multifields = GetMultiFieldDtos(fmSource);
        var existingEntries = targetCollection.ToList();

        foreach (var multifield in multifields)
        {
            if (multifield.Value is null)
            {
                continue;
            }
            var existingMultiField = targetCollection
                .FirstOrDefault(m => m.FmMultiField?.Name == multifield.Name
                    && m.FmMultiFieldValue?.Value == multifield.Value);

            if (existingMultiField is not null)
            {
                ArgumentNullException.ThrowIfNull(existingMultiField.FmMultiField);
                ArgumentNullException.ThrowIfNull(existingMultiField.FmMultiFieldValue);
                existingMultiField.FmMultiFieldValue.Order = multifield.Order;
                existingEntries.Remove(existingMultiField);
            }
            else
            {
                T fmTargetMultiField = new()
                {
                    FmMultiField = new() { Name = multifield.Name },
                    FmMultiFieldValue = new() { Value = multifield.Value, Order = multifield.Order }
                };

                targetCollection.Add(fmTargetMultiField);
            }
        }
        foreach (var entry in existingEntries)
        {
            targetCollection.Remove(entry);
        }
    }

    private static List<MultiFieldDto> GetMultiFieldDtos(object fmSource)
    {
        List<MultiFieldDto> dtos = [];
        var sourceProperties = fmSource.GetType().GetProperties();
        foreach (var prop in sourceProperties)
        {
            if (prop.GetCustomAttributes(typeof(FileMakerMultiFieldAttribute), false)
                                 .FirstOrDefault() is FileMakerMultiFieldAttribute attribute)
            {
                dtos.Add(new(attribute.MultiFieldName, prop.GetValue(fmSource)?.ToString(), attribute.Order));
            }
        }
        return dtos;
    }
}

internal sealed record MultiFieldDto(string Name, string? Value, int Order);
