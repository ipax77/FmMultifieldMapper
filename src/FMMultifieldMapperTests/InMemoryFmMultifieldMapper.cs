using FMMultifieldMapper;
using Microsoft.EntityFrameworkCore;

namespace FMMultifieldMapperTests;

internal class InMemoryFmMultifieldMapper(TestContext context) : FmMultifieldMapper
{
    public override async Task<int> GetOrCreateMultifieldId(string name)
    {
        var id = await context.Multifields
            .Where(x => x.Name == name)
            .Select(s => s.FmMultifieldId)
            .FirstOrDefaultAsync();

        if (id == 0)
        {
            var multifield = new FmMultifield() { Name = name };
            context.Multifields.Add(multifield);
            await context.SaveChangesAsync();
            id = multifield.FmMultifieldId;
        }
        return id;
    }

    public override async Task<int> GetOrCreateMultifieldValueId(int multifieldId, string value)
    {
        var id = await context.MultifieldValues
            .Where(x => x.FmMultifieldId == multifieldId && x.Value == value)
            .Select(s => s.FmMultifieldValueId)
            .FirstOrDefaultAsync();

        if (id == 0)
        {
            var multifieldValue = new FmMultifieldValue()
            {
                FmMultifieldId = multifieldId,
                Value = value
            };
            context.MultifieldValues.Add(multifieldValue);
            await context.SaveChangesAsync();
            id = multifieldValue.FmMultifieldValueId;
        }
        return id;
    }
}

internal class CacheFmMultifieldMapper(TestContext context) : FmMultifieldMapper
{
    private bool isInit;
    private Dictionary<string, int> multifields = [];
    private Dictionary<MultifieldValueKey, int> multifieldValues = [];
    private SemaphoreSlim ss = new(1, 1);

    private async Task Init()
    {
        if (isInit)
        {
            return;
        }
        await ss.WaitAsync();
        try
        {
            if (!isInit)
            {
                multifields = (await context.Multifields.ToListAsync())
                    .ToDictionary(k => k.Name, v => v.FmMultifieldId);
                multifieldValues = (await context.MultifieldValues.ToListAsync())
                    .ToDictionary(k => new MultifieldValueKey(k.FmMultifieldId, k.Value), v => v.FmMultifieldValueId);
                isInit = true;
            }
        }
        finally
        {
            ss.Release();
        }
    }

    public override async Task<int> GetOrCreateMultifieldId(string name)
    {
        await Init();
        if (!multifields.TryGetValue(name, out var id))
        {
            var multifield = new FmMultifield() { Name = name };
            context.Multifields.Add(multifield);
            await context.SaveChangesAsync();
            id = multifields[name] = multifield.FmMultifieldId;
        }
        return id;
    }

    public override async Task<int> GetOrCreateMultifieldValueId(int multifieldId, string value)
    {
        await Init();
        var key = new MultifieldValueKey(multifieldId, value);
        if (!multifieldValues.TryGetValue(key, out var id))
        {
            var multifieldValue = new FmMultifieldValue()
            {
                FmMultifieldId = multifieldId,
                Value = value
            };
            context.MultifieldValues.Add(multifieldValue);
            await context.SaveChangesAsync();
            id = multifieldValues[key] = multifieldValue.FmMultifieldValueId;
        }
        return id;
    }
}

internal record MultifieldValueKey(int MultifieldId, string Value);