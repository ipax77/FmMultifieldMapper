using FMMultiFieldMapper;
using Microsoft.EntityFrameworkCore;

namespace FMMultifieldMapperTests;

public class InMemoryFmMultiFieldMapper(DbTestContext context) : FmMultiFieldMap
{
    public override async Task<int> GetOrCreateMultiFieldId(string name)
    {
        var id = await context.Multifields
            .Where(x => x.Name == name)
            .Select(s => s.FmMultiFieldId)
            .FirstOrDefaultAsync();

        if (id == 0)
        {
            var multifield = new FmMultiField() { Name = name };
            context.Multifields.Add(multifield);
            await context.SaveChangesAsync();
            id = multifield.FmMultiFieldId;
        }
        return id;
    }

    public override async Task<int> GetOrCreateMultiFieldValueId(int multifieldId, string value)
    {
        var id = await context.MultifieldValues
            .Where(x => x.FmMultiFieldId == multifieldId && x.Value == value)
            .Select(s => s.FmMultiFieldValueId)
            .FirstOrDefaultAsync();

        if (id == 0)
        {
            var multifieldValue = new FmMultiFieldValue()
            {
                FmMultiFieldId = multifieldId,
                Value = value
            };
            context.MultifieldValues.Add(multifieldValue);
            await context.SaveChangesAsync();
            id = multifieldValue.FmMultiFieldValueId;
        }
        return id;
    }

    protected override async Task<IReadOnlyDictionary<string, int>> GetOrCreateMultiFieldIds(
        IReadOnlyCollection<string> names)
    {
        var distinctNames = names.ToHashSet(StringComparer.Ordinal);
        var entities = (await context.Multifields
                .Where(x => distinctNames.Contains(x.Name))
                .ToListAsync())
            .ToDictionary(x => x.Name, StringComparer.Ordinal);

        var hasNewEntities = false;
        foreach (var name in distinctNames)
        {
            if (!entities.ContainsKey(name))
            {
                var entity = new FmMultiField { Name = name };
                context.Multifields.Add(entity);
                entities.Add(name, entity);
                hasNewEntities = true;
            }
        }

        if (hasNewEntities)
        {
            await context.SaveChangesAsync();
        }

        return entities.ToDictionary(x => x.Key, x => x.Value.FmMultiFieldId, StringComparer.Ordinal);
    }

    protected override async Task<IReadOnlyDictionary<(int MultiFieldId, string Value), int>>
        GetOrCreateMultiFieldValueIds(IReadOnlyCollection<(int MultiFieldId, string Value)> values)
    {
        var distinctValues = values.ToHashSet();
        var multiFieldIds = distinctValues.Select(x => x.MultiFieldId).ToHashSet();
        var entities = (await context.MultifieldValues
                .Where(x => multiFieldIds.Contains(x.FmMultiFieldId))
                .ToListAsync())
            .Where(x => distinctValues.Contains((x.FmMultiFieldId, x.Value)))
            .ToDictionary(x => (x.FmMultiFieldId, x.Value));

        var hasNewEntities = false;
        foreach (var value in distinctValues)
        {
            if (!entities.ContainsKey(value))
            {
                var entity = new FmMultiFieldValue
                {
                    FmMultiFieldId = value.MultiFieldId,
                    Value = value.Value
                };
                context.MultifieldValues.Add(entity);
                entities.Add(value, entity);
                hasNewEntities = true;
            }
        }

        if (hasNewEntities)
        {
            await context.SaveChangesAsync();
        }

        return entities.ToDictionary(x => x.Key, x => x.Value.FmMultiFieldValueId);
    }
}

public class CacheFmMultiFieldMapper(DbTestContext context) : FmMultiFieldMap
{
    private bool isInit;
    private Dictionary<string, int> multifields = [];
    private Dictionary<MultifieldValueKey, int> multifieldValues = [];
    private readonly SemaphoreSlim initSs = new(1, 1);
    private readonly SemaphoreSlim createSs = new(1, 1);

    private async Task Init()
    {
        if (isInit)
        {
            return;
        }
        await initSs.WaitAsync();
        try
        {
            if (!isInit)
            {
                multifields = (await context.Multifields.ToListAsync())
                    .ToDictionary(k => k.Name, v => v.FmMultiFieldId);
                multifieldValues = (await context.MultifieldValues.ToListAsync())
                    .ToDictionary(k => new MultifieldValueKey(k.FmMultiFieldId, k.Value), v => v.FmMultiFieldValueId);
                isInit = true;
            }
        }
        finally
        {
            initSs.Release();
        }
    }

    public override async Task<int> GetOrCreateMultiFieldId(string name)
    {
        await Init();
        if (!multifields.TryGetValue(name, out var id))
        {
            await createSs.WaitAsync();
            try
            {
                var multifield = new FmMultiField() { Name = name };
                context.Multifields.Add(multifield);
                await context.SaveChangesAsync();
                id = multifields[name] = multifield.FmMultiFieldId;
            }
            finally
            {
                createSs.Release();
            }
        }
        return id;
    }

    public override async Task<int> GetOrCreateMultiFieldValueId(int multifieldId, string value)
    {
        await Init();
        var key = new MultifieldValueKey(multifieldId, value);
        if (!multifieldValues.TryGetValue(key, out var id))
        {
            await createSs.WaitAsync();
            try
            {
                var multifieldValue = new FmMultiFieldValue()
                {
                    FmMultiFieldId = multifieldId,
                    Value = value
                };
                context.MultifieldValues.Add(multifieldValue);
                await context.SaveChangesAsync();
                id = multifieldValues[key] = multifieldValue.FmMultiFieldValueId;
            }
            finally
            {
                createSs.Release();
            }
        }
        return id;
    }
}

internal record MultifieldValueKey(int MultifieldId, string Value);
