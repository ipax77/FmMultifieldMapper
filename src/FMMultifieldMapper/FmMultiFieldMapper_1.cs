namespace FMMultiFieldMapper;

/// <summary>
/// FmMultiFieldMap
/// </summary>
public abstract class FmMultiFieldMap
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
    /// <param name="order">MultifieldValue value</param>
    /// <returns></returns>
    public abstract Task<int> GetOrCreateMultifieldValueId(int multifieldId, string value, int order);

    /// <summary>
    /// Map sourceCollection to fmObject FmMultifield attribute properties
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sourceCollection"></param>
    /// <param name="fmTarget"></param>
    public static void MapToFmObject<T>(ICollection<T> sourceCollection, object fmTarget) where T : IFmTargetMultiField, new()
    {
        ArgumentNullException.ThrowIfNull(sourceCollection);

        Dictionary<string, List<string>> fmMultifields = [];
        foreach (var group in sourceCollection.GroupBy(g => g.FmMultiField?.Name))
        {
            var name = group.Key;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }
            var list = group
                .OrderBy(o => o.FmMultiFieldValue?.Order)
                .Select(s => s.FmMultiFieldValue?.Value ?? string.Empty)
                .ToList();
            fmMultifields[name] = list;
        }
        MapToFmObject(fmMultifields, fmTarget);
    }

    /// <summary>
    /// Map fmMultifields to fmObject FmMultifield attribute properties
    /// </summary>
    /// <param name="fmMultifields"></param>
    /// <param name="fmTarget"></param>
    public static void MapToFmObject(Dictionary<string, List<string>> fmMultifields, object fmTarget)
    {
        ArgumentNullException.ThrowIfNull(fmMultifields);
        ArgumentNullException.ThrowIfNull(fmTarget);

        var targetProperties = fmTarget.GetType().GetProperties();

        foreach (var property in targetProperties)
        {
            var attribute = (FileMakerMultifieldAttribute?)property
                .GetCustomAttributes(typeof(FileMakerMultifieldAttribute), false)
                .FirstOrDefault();

            if (attribute == null)
            {
                continue;
            }

            if (fmMultifields.TryGetValue(attribute.MultifieldName, out var values)
                && values.Count > attribute.Order)
            {
                property.SetValue(fmTarget, values[attribute.Order]);
            }
            else
            {
                property.SetValue(fmTarget, string.Empty);
            }
        }
    }

    /// <summary>
    /// Map FmMultifields to Target collection using existing and creating new Multifields/MultifieldValues
    /// </summary>
    /// <typeparam name="T">The type of the target multifield, implementing IFmTargetMultifield</typeparam>
    /// <param name="fmSource">FileMaker source with FmMultifield attributes</param>
    /// <param name="targetCollection">IFmTargetMultifield collection</param>
    public async Task Map<T>(object fmSource, ICollection<T> targetCollection) where T : IFmTargetMultiField, new()
    {
        ArgumentNullException.ThrowIfNull(fmSource);
        ArgumentNullException.ThrowIfNull(targetCollection);

        var targetMultifields = GetMultifieldDtos(fmSource);
        await MapMultifields(targetMultifields, targetCollection).ConfigureAwait(false);
    }

    private async Task MapMultifields<T>(List<MultifieldDto> targetMultifields, ICollection<T> targetCollection)
        where T : IFmTargetMultiField, new()
    {
        var existingEntries = targetCollection.ToList();

        foreach (var multifieldGroup in targetMultifields.GroupBy(g => g.Name))
        {
            var multifieldName = multifieldGroup.First().Name;
            var multifieldId = await GetOrCreateMultifieldId(multifieldName)
                .ConfigureAwait(false);

            foreach (var targetMultifield in multifieldGroup)
            {
                if (targetMultifield is null || string.IsNullOrEmpty(targetMultifield.Value))
                {
                    continue;
                }

                var multifieldValue = targetMultifield.Value;
                var multifieldValueId = await GetOrCreateMultifieldValueId(multifieldId, multifieldValue,
                    targetMultifield.Order).ConfigureAwait(false);

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
                        FmMultiFieldId = multifieldId,
                        FmMultiFieldValueId = multifieldValueId
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

    private static List<MultifieldDto> GetMultifieldDtos(Dictionary<string, List<string>> fmMultifields)
    {
        List<MultifieldDto> dtos = [];

        foreach (var ent in fmMultifields)
        {
            var name = ent.Key;
            for (int i = 0; i < ent.Value.Count; i++)
            {
                dtos.Add(new(name, ent.Value[i], i + 1));
            }
        }

        return dtos;
    }

    /// <summary>
    /// Maps the targetCollection to a dictionary of multifield names to their values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="targetCollection"></param>
    /// <param name="dtoMultifields"></param>
    public static void MapToDtoDictionary<T>(ICollection<T> targetCollection, Dictionary<string, List<string>> dtoMultifields) where T : IFmTargetMultiField, new()
    {
        ArgumentNullException.ThrowIfNull(targetCollection);
        ArgumentNullException.ThrowIfNull(dtoMultifields);

        foreach (var group in targetCollection.GroupBy(g => g.FmMultiField?.Name))
        {
            if (string.IsNullOrEmpty(group.Key))
            {
                continue;
            }
            List<string> values = group
                .OrderBy(o => o.FmMultiFieldValue?.Order)
                .Select(s => s.FmMultiFieldValue?.Value ?? string.Empty)
                .ToList();
            dtoMultifields[group.Key] = values;
        }
    }

    /// <summary>
    /// Maps a dictionary of multifield names to values to the target collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dtoMultifields"></param>
    /// <param name="targetCollection"></param>
    /// <returns></returns>
    public async Task MapFromDtoDictionary<T>(Dictionary<string, List<string>> dtoMultifields, ICollection<T> targetCollection)
        where T : IFmTargetMultiField, new()
    {
        ArgumentNullException.ThrowIfNull(dtoMultifields);
        ArgumentNullException.ThrowIfNull(targetCollection);

        var targetMultifields = GetMultifieldDtos(dtoMultifields);
        var existingEntries = targetCollection.ToList();
        await MapMultifields(targetMultifields, targetCollection).ConfigureAwait(false);
    }

    /// <summary>
    /// AssertFmTargetObjectIsValid
    /// </summary>
    /// <param name="fmTarget"></param>
    /// <exception cref="InvalidOperationException"></exception>
    public static void AssertFmTargetObjectIsValid(object fmTarget)
    {
        ArgumentNullException.ThrowIfNull(fmTarget);

        var targetProperties = fmTarget.GetType().GetProperties();
        var multifieldAttributes = targetProperties
            .Select(prop => new
            {
                Property = prop,
                Attribute = (FileMakerMultifieldAttribute?)prop
                    .GetCustomAttributes(typeof(FileMakerMultifieldAttribute), false)
                    .FirstOrDefault()
            })
            .Where(x => x.Attribute != null)
            .ToList();

        // Ensure there is at least one FileMakerMultifieldAttribute
        if (multifieldAttributes.Count == 0)
        {
            throw new InvalidOperationException("The target object does not contain any properties with the FileMakerMultifieldAttribute.");
        }

        // Group by MultifieldName and check the Order consistency
        var groupedAttributes = multifieldAttributes
            .GroupBy(x => x.Attribute!.MultifieldName)
            .ToList();

        foreach (var group in groupedAttributes)
        {
            var orders = group
                .Select(x => x.Attribute!.Order)
                .OrderBy(order => order)
                .ToList();

            // Check if orders start from 0 and have no gaps
            for (int i = 0; i < orders.Count; i++)
            {
                if (orders[i] != i)
                {
                    throw new InvalidOperationException($"The MultifieldName '{group.Key}' does not have a consistent order starting from 0 with no gaps. The expected order at position {i} is {i}, but found {orders[i]}.");
                }
            }
        }
    }
}
