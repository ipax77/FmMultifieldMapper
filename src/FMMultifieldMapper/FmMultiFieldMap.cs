using FMMultiFieldMapper.Sync;

namespace FMMultiFieldMapper;

/// <summary>
/// FmMultiFieldMap
/// </summary>
public abstract class FmMultiFieldMap
{
    /// <summary>
    /// GetOrCreateMultiFieldId
    /// </summary>
    /// <param name="name">MultiField name</param>
    /// <returns></returns>
    public abstract Task<int> GetOrCreateMultiFieldId(string name);
    /// <summary>
    /// GetOrCreateMultiFieldValueId
    /// </summary>
    /// <param name="multifieldId">Linked MultiFieldId</param>
    /// <param name="value">MultiFieldValue value</param>
    /// <returns></returns>
    public abstract Task<int> GetOrCreateMultiFieldValueId(int multifieldId, string value);

    /// <summary>
    /// Map sourceCollection to fmObject FmMultiField attribute properties
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="sourceCollection"></param>
    /// <param name="fmTarget"></param>
    public static void MapToFmObject<T>(ICollection<T> sourceCollection, object fmTarget) where T : IFmTargetMultiField, new()
    {
        ArgumentNullException.ThrowIfNull(sourceCollection);

        Dictionary<string, List<string>> fmMultiFields = [];
        foreach (var group in sourceCollection.GroupBy(g => g.FmMultiField?.Name))
        {
            var name = group.Key;
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }
            var list = group
                .OrderBy(o => o.Order)
                .Select(s => s.FmMultiFieldValue?.Value ?? string.Empty)
                .ToList();
            fmMultiFields[name] = list;
        }
        MapToFmObject(fmMultiFields, fmTarget);
    }

    /// <summary>
    /// Map fmMultiFields to fmObject FmMultiField attribute properties
    /// </summary>
    /// <param name="fmMultiFields"></param>
    /// <param name="fmTarget"></param>
    public static void MapToFmObject(Dictionary<string, List<string>> fmMultiFields, object fmTarget)
    {
        ArgumentNullException.ThrowIfNull(fmMultiFields);
        ArgumentNullException.ThrowIfNull(fmTarget);

        var targetProperties = fmTarget.GetType().GetProperties();

        foreach (var property in targetProperties)
        {
            var attribute = (FileMakerMultiFieldAttribute?)property
                .GetCustomAttributes(typeof(FileMakerMultiFieldAttribute), false)
                .FirstOrDefault();

            if (attribute == null)
            {
                continue;
            }

            if (fmMultiFields.TryGetValue(attribute.MultiFieldName, out var values)
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
    /// Map FmMultiFields to Target collection using existing and creating new MultiFields/MultiFieldValues
    /// </summary>
    /// <typeparam name="T">The type of the target multifield, implementing IFmTargetMultiField</typeparam>
    /// <param name="fmSource">FileMaker source with FmMultiField attributes</param>
    /// <param name="targetCollection">IFmTargetMultiField collection</param>
    public async Task Map<T>(object fmSource, ICollection<T> targetCollection) where T : IFmTargetMultiField, new()
    {
        ArgumentNullException.ThrowIfNull(fmSource);
        ArgumentNullException.ThrowIfNull(targetCollection);

        var targetMultiFields = GetMultiFieldDtos(fmSource);
        await MapMultiFields(targetMultiFields, targetCollection).ConfigureAwait(false);
    }

    private async Task MapMultiFields<T>(List<MultiFieldDto> targetMultiFields, ICollection<T> targetCollection)
        where T : IFmTargetMultiField, new()
    {
        var existingEntries = targetCollection.ToList();

        foreach (var multifieldGroup in targetMultiFields.GroupBy(g => g.Name))
        {
            var multifieldName = multifieldGroup.First().Name;
            var multifieldId = await GetOrCreateMultiFieldId(multifieldName)
                .ConfigureAwait(false);

            foreach (var targetMultiField in multifieldGroup)
            {
                if (targetMultiField is null || string.IsNullOrEmpty(targetMultiField.Value))
                {
                    continue;
                }

                var multifieldValue = targetMultiField.Value;
                var multifieldValueId = await GetOrCreateMultiFieldValueId(multifieldId, multifieldValue)
                        .ConfigureAwait(false);

                var existingMultiField = targetCollection
                    .FirstOrDefault(m => m.FmMultiField?.Name == targetMultiField.Name
                        && m.FmMultiFieldValue?.Value == targetMultiField.Value);

                if (existingMultiField is not null)
                {
                    ArgumentNullException.ThrowIfNull(existingMultiField.FmMultiField);
                    ArgumentNullException.ThrowIfNull(existingMultiField.FmMultiFieldValue);
                    existingMultiField.Order = targetMultiField.Order;
                    existingEntries.Remove(existingMultiField);
                }
                else
                {
                    T fmTargetMultiField = new()
                    {
                        FmMultiFieldId = multifieldId,
                        FmMultiFieldValueId = multifieldValueId
                    };
                    targetCollection.Add(fmTargetMultiField);
                }
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

    private static List<MultiFieldDto> GetMultiFieldDtos(Dictionary<string, List<string>> fmMultiFields)
    {
        List<MultiFieldDto> dtos = [];

        foreach (var ent in fmMultiFields)
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
    /// Maps the targetCollection to a dictionary of multifield names and their values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="targetCollection"></param>
    public static Dictionary<string, List<string>> GetDtoDictionary<T>(ICollection<T> targetCollection)
        where T : IFmTargetMultiField, new()
    {
        ArgumentNullException.ThrowIfNull(targetCollection);
        var dtoMultiFields = new Dictionary<string, List<string>>();

        foreach (var group in targetCollection.GroupBy(g => g.FmMultiField?.Name))
        {
            if (string.IsNullOrEmpty(group.Key))
            {
                continue;
            }
            List<string> values = group
                .OrderBy(o => o.Order)
                .Select(s => s.FmMultiFieldValue?.Value ?? string.Empty)
                .ToList();
            dtoMultiFields[group.Key] = values;
        }
        return dtoMultiFields;
    }

    /// <summary>
    /// Maps fmObject multi-fields to a dictionary of multifield names and their values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="fmObject"></param>
    public static Dictionary<string, List<string>> GetDtoDictionary<T>(T fmObject) where T : IFmObject, new()
    {
        ArgumentNullException.ThrowIfNull(fmObject);
        var dtoMultiFields = new Dictionary<string, List<string>>();

        var targetMultiFields = GetMultiFieldDtos(fmObject);
        foreach (var group in targetMultiFields
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .GroupBy(g => g.Name))
        {
            if (string.IsNullOrEmpty(group.Key))
            {
                continue;
            }
            List<string> values = group
                .OrderBy(o => o.Order)
                .Select(s => s.Value ?? string.Empty)
                .ToList();
            dtoMultiFields[group.Key] = values;
        }

        return dtoMultiFields;
    }

    /// <summary>
    /// Maps the targetCollection to a dictionary of multifield names to their values.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="targetCollection"></param>
    /// <param name="dtoMultiFields"></param>
    [Obsolete ( message: "Use GetDtoDictionary instead")]
    public static void MapToDtoDictionary<T>(ICollection<T> targetCollection, Dictionary<string, List<string>> dtoMultiFields) 
        where T : IFmTargetMultiField, new()
    {
        ArgumentNullException.ThrowIfNull(targetCollection);
        ArgumentNullException.ThrowIfNull(dtoMultiFields);

        foreach (var group in targetCollection.GroupBy(g => g.FmMultiField?.Name))
        {
            if (string.IsNullOrEmpty(group.Key))
            {
                continue;
            }
            List<string> values = group
                .OrderBy(o => o.Order)
                .Select(s => s.FmMultiFieldValue?.Value ?? string.Empty)
                .ToList();
            dtoMultiFields[group.Key] = values;
        }
    }

    /// <summary>
    /// Maps a dictionary of multifield names to values to the target collection.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="dtoMultiFields"></param>
    /// <param name="targetCollection"></param>
    /// <returns></returns>
    public async Task MapFromDtoDictionary<T>(Dictionary<string, List<string>> dtoMultiFields, ICollection<T> targetCollection)
        where T : IFmTargetMultiField, new()
    {
        ArgumentNullException.ThrowIfNull(dtoMultiFields);
        ArgumentNullException.ThrowIfNull(targetCollection);

        var targetMultiFields = GetMultiFieldDtos(dtoMultiFields);
        await MapMultiFields(targetMultiFields, targetCollection).ConfigureAwait(false);
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
                Attribute = (FileMakerMultiFieldAttribute?)prop
                    .GetCustomAttributes(typeof(FileMakerMultiFieldAttribute), false)
                    .FirstOrDefault()
            })
            .Where(x => x.Attribute != null)
            .ToList();

        // Ensure there is at least one FileMakerMultiFieldAttribute
        if (multifieldAttributes.Count == 0)
        {
            throw new InvalidOperationException("The target object does not contain any properties with the FileMakerMultiFieldAttribute.");
        }

        // Group by MultiFieldName and check the Order consistency
        var groupedAttributes = multifieldAttributes
            .GroupBy(x => x.Attribute!.MultiFieldName)
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
                    throw new InvalidOperationException($"The MultiFieldName '{group.Key}' does not have a consistent order starting from 0 with no gaps. The expected order at position {i} is {i}, but found {orders[i]}.");
                }
            }
        }
    }
}
