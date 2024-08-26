namespace FMMultifieldMapper;

/// <summary>
/// FmMapper
/// </summary>
public static class FmMapper
{
    /// <summary>
    /// Map FmMultifield to Target collection
    /// </summary>
    /// <typeparam name="T">The type of the target multifield, implementing IFmTargetMultifield</typeparam>
    /// <param name="fmSource">FileMaker source with FmMultifield attributes</param>
    /// <param name="targetCollection">IFmTargetMultifield collection</param>
    public static void Map<T>(object fmSource, ICollection<T> targetCollection) where T : IFmTargetMultifield, new()
    {
        ArgumentNullException.ThrowIfNull(fmSource);
        ArgumentNullException.ThrowIfNull(targetCollection);

        var multifields = GetMultifieldDtos(fmSource);

        foreach (var multifield in multifields)
        {
            T fmTargetMultifield = new()
            {
                FmMultiField = new() { Name = multifield.Name },
                FmMultiFieldValue = new() { Value = multifield.Value, Order = multifield.Order }
            };
            targetCollection.Add(fmTargetMultifield);
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

internal sealed record MultifieldDto(string Name, string? Value, int Order);
