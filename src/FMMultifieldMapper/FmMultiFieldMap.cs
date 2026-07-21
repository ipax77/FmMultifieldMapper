using System.Reflection;
using System.Runtime.CompilerServices;
using FMMultiFieldMapper.Sync;

namespace FMMultiFieldMapper;

/// <summary>
/// FmMultiFieldMap
/// </summary>
public abstract class FmMultiFieldMap
{
    private static readonly ConditionalWeakTable<Type, MultiFieldPropertyMetadata[]> PropertyMetadataCache = new();

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
    /// Resolves IDs for a batch of multi-field names. Override this method to use a set-based data store operation.
    /// </summary>
    /// <param name="names">The distinct multi-field names to resolve.</param>
    /// <returns>IDs keyed by multi-field name.</returns>
    protected virtual async Task<IReadOnlyDictionary<string, int>> GetOrCreateMultiFieldIds(
        IReadOnlyCollection<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);

        var ids = new Dictionary<string, int>(names.Count, StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (!ids.ContainsKey(name))
            {
                ids.Add(name, await GetOrCreateMultiFieldId(name).ConfigureAwait(false));
            }
        }

        return ids;
    }

    /// <summary>
    /// Resolves IDs for a batch of multi-field values. Override this method to use a set-based data store operation.
    /// </summary>
    /// <param name="values">The distinct multi-field ID and value pairs to resolve.</param>
    /// <returns>IDs keyed by multi-field ID and value.</returns>
    protected virtual async Task<IReadOnlyDictionary<(int MultiFieldId, string Value), int>> GetOrCreateMultiFieldValueIds(
        IReadOnlyCollection<(int MultiFieldId, string Value)> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var ids = new Dictionary<(int MultiFieldId, string Value), int>(values.Count);
        foreach (var value in values)
        {
            if (!ids.ContainsKey(value))
            {
                ids.Add(
                    value,
                    await GetOrCreateMultiFieldValueId(value.MultiFieldId, value.Value).ConfigureAwait(false));
            }
        }

        return ids;
    }

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

        foreach (var metadata in GetMultiFieldPropertyMetadata(fmTarget.GetType()))
        {
            var property = metadata.Property;
            var attribute = metadata.Attribute;

            if (fmMultiFields.TryGetValue(attribute.MultiFieldName, out var values))
            {
                if (attribute.IsSpecialField)
                {
                    property.SetValue(fmTarget, string.Join("\r\n", values) + "\r\n");
                }
                else if (values.Count > attribute.Order)
                {
                    property.SetValue(fmTarget, values[attribute.Order]);
                }
                else
                {
                    property.SetValue(fmTarget, string.Empty);
                }
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
        HashSet<T> existingEntries = [.. targetCollection];
        var existingByKey = new Dictionary<MultiFieldKey, Queue<T>>();
        foreach (var entry in targetCollection.OrderBy(x => x.Order))
        {
            if (entry.FmMultiField is not null && entry.FmMultiFieldValue is not null)
            {
                var key = new MultiFieldKey(entry.FmMultiField.Name, entry.FmMultiFieldValue.Value);
                if (!existingByKey.TryGetValue(key, out var entries))
                {
                    entries = new Queue<T>();
                    existingByKey[key] = entries;
                }
                entries.Enqueue(entry);
            }
        }

        var distinctNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var targetMultiField in targetMultiFields)
        {
            distinctNames.Add(targetMultiField.Name);
        }

        var multiFieldIds = await GetOrCreateMultiFieldIds(distinctNames).ConfigureAwait(false);
        foreach (var name in distinctNames)
        {
            if (!multiFieldIds.ContainsKey(name))
            {
                throw new InvalidOperationException(
                    $"The bulk multi-field resolver did not return an ID for name '{name}'.");
            }
        }

        var distinctValues = new HashSet<(int MultiFieldId, string Value)>();
        foreach (var targetMultiField in targetMultiFields)
        {
            if (!string.IsNullOrEmpty(targetMultiField.Value))
            {
                distinctValues.Add((multiFieldIds[targetMultiField.Name], targetMultiField.Value));
            }
        }

        var multiFieldValueIds = await GetOrCreateMultiFieldValueIds(distinctValues).ConfigureAwait(false);
        foreach (var value in distinctValues)
        {
            if (!multiFieldValueIds.ContainsKey(value))
            {
                throw new InvalidOperationException(
                    $"The bulk multi-field value resolver did not return an ID for multi-field ID " +
                    $"'{value.MultiFieldId}' and value '{value.Value}'.");
            }
        }

        foreach (var targetMultiField in targetMultiFields)
        {
            if (string.IsNullOrEmpty(targetMultiField.Value))
            {
                continue;
            }

            var multifieldId = multiFieldIds[targetMultiField.Name];
            var multifieldValueId = multiFieldValueIds[(multifieldId, targetMultiField.Value)];
            var key = new MultiFieldKey(targetMultiField.Name, targetMultiField.Value);

            if (existingByKey.TryGetValue(key, out var existingMultiFields)
                && existingMultiFields.Count > 0)
            {
                var existingMultiField = existingMultiFields.Dequeue();
                existingMultiField.Order = targetMultiField.Order;
                existingEntries.Remove(existingMultiField);
            }
            else
            {
                T fmTargetMultiField = new()
                {
                    FmMultiFieldId = multifieldId,
                    FmMultiFieldValueId = multifieldValueId,
                    Order = targetMultiField.Order
                };
                targetCollection.Add(fmTargetMultiField);
            }
        }

        foreach (var entry in existingEntries)
        {
            targetCollection.Remove(entry);
        }
    }

    internal static List<MultiFieldDto> GetMultiFieldDtos(object fmSource)
    {
        List<MultiFieldDto> dtos = [];
        foreach (var metadata in GetMultiFieldPropertyMetadata(fmSource.GetType()))
        {
            var value = metadata.Property.GetValue(fmSource)?.ToString();
            var attribute = metadata.Attribute;

            if (attribute.IsSpecialField && value != null)
            {
                // Handle special fields by splitting newline-separated values
                var values = value.Split(["\r\n", "\r", "\n"], StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < values.Length; i++)
                {
                    dtos.Add(new MultiFieldDto(attribute.MultiFieldName, values[i].Trim(), i));
                }
            }
            else
            {
                // Normal multi-fields
                dtos.Add(new MultiFieldDto(attribute.MultiFieldName, value, attribute.Order));
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
                dtos.Add(new(name, ent.Value[i], i));
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
    [Obsolete(message: "Use GetDtoDictionary instead")]
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

        var multifieldAttributes = GetMultiFieldPropertyMetadata(fmTarget.GetType());

        // Ensure there is at least one FileMakerMultiFieldAttribute
        if (multifieldAttributes.Length == 0)
        {
            throw new InvalidOperationException("The target object does not contain any properties with the FileMakerMultiFieldAttribute.");
        }

        // Group by MultiFieldName and check the Order consistency
        var groupedAttributes = multifieldAttributes
            .GroupBy(x => x.Attribute.MultiFieldName)
            .ToList();

        foreach (var group in groupedAttributes)
        {
            var orders = group
                .Select(x => x.Attribute.Order)
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

    private static MultiFieldPropertyMetadata[] GetMultiFieldPropertyMetadata(Type type)
    {
        return PropertyMetadataCache.GetValue(type, static objectType =>
        {
            var metadata = new List<MultiFieldPropertyMetadata>();
            foreach (var property in objectType.GetProperties())
            {
                var attribute = property.GetCustomAttribute<FileMakerMultiFieldAttribute>(inherit: false);
                if (attribute is not null)
                {
                    metadata.Add(new MultiFieldPropertyMetadata(property, attribute));
                }
            }

            return [.. metadata];
        });
    }
}

internal sealed record MultiFieldPropertyMetadata(
    PropertyInfo Property,
    FileMakerMultiFieldAttribute Attribute);

internal readonly record struct MultiFieldKey(string Name, string Value);
