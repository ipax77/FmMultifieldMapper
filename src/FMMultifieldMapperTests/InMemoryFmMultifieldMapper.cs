using FMMultiFieldMapper;
using Microsoft.EntityFrameworkCore;

namespace FMMultifieldMapperTests;

internal class InMemoryFmMultiFieldMapper(TestContext context) : FmMultiFieldMap
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
}

internal class CacheFmMultiFieldMapper(TestContext context) : FmMultiFieldMap
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