[![build and test](https://github.com/ipax77/FmMultifieldMapper/actions/workflows/tests.yml/badge.svg)](https://github.com/ipax77/FmMultifieldMapper/actions/workflows/tests.yml)

# FileMaker MultiField mapper

This library maps data between [FileMaker](https://www.claris.com/)-based DTO objects and relational database objects. The focus is on multi-fields that are easily filterable.

## Installation

You can install the library via NuGet:
```
dotnet add package FMMultiFieldMapper
```

## Sample Usage

### Implementing `FmMultiFieldMap`

To use the `FmMultiFieldMapper`, you need to implement the abstract `FmMultiFieldMap` class. 

```csharp
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
```

### Mapping FileMaker Objects to Relational Database Objects

Below is an example of how to map a FileMaker object (FmSourceTestClass) to a relational database object (FmTargetTestClassMultifield).

```csharp
[DataContract(Name = "TestLayout")]
public class FmSourceTestClass
{
    [NotMapped]
    public int FileMakerRecordId { get; set; }
    [DataMember(Name = "Themen(1)")]
    [FileMakerMultiField(MultiFieldName = "Themen", Order = 0)]
    public string? Themen1 { get; set; }
    [DataMember(Name = "Themen(2)")]
    [FileMakerMultiField(MultiFieldName = "Themen", Order = 1)]
    public string? Themen2 { get; set; }
    [DataMember(Name = "Themen(3)")]
    [FileMakerMultiField(MultiFieldName = "Themen", Order = 2)]
    public string? Themen3 { get; set; }
}

public class FmTargetTestClassMultifield : IFmTargetMultiField
{
    public int FmTargetTestClassMultifieldId { get; set; }
    public int FmMultiFieldId { get; set; }
    public FmMultiField? FmMultiField { get; set; }
    public int FmMultiFieldValueId { get; set; }
    public FmMultiFieldValue? FmMultiFieldValue { get; set; }
    public int FmTargetTestClassId { get; set; }
    public FmTargetTestClass? FmTargetTestClass { get; set; }
    public int Order { get; set; }
}

var target = _dbContext.FmTargetTestClasses.FirstOrDefault();

var source = new FmSourceTestClass()
{
    Themen1 = "Test3",
    Themen2 = "Test4",
    Themen3 = "Test5"
};

InMemoryFmMultiFieldMapper mapper = new(_dbContext);
await mapper.Map(source, target.FmTargetTestClassMultifields);
_dbContext.SaveChanges();
```

## DTO mapping

You can also map data from a DTO object to your database entities using `Dictionary<string, List<string>>`

Here's how you can map a `FmTargetTestClassDto` to `FmTargetTestClass`:

```csharp
FmTargetTestClassDto dto = new()
{
    FmTargetTestClassMultifields = new()
    {
        { "Themen", ["Test1", "Test2", "Test3"] },
        { "Was", ["WTest1", "WTest2", "WTest3"] },
    }
};

CacheFmMultiFieldMapper mapper = new(_dbContext);

FmTargetTestClass fmTargetTestClass = new();
_dbContext.FmTargetTestClasses.Add(fmTargetTestClass);
_dbContext.SaveChanges();

await mapper.MapFromDtoDictionary(dto.FmTargetTestClassMultifields, fmTargetTestClass.FmTargetTestClassMultifields);
_dbContext.SaveChanges();

var fmTargetTestClassWithIncludes = _dbContext.FmTargetTestClasses
    .Include(i => i.FmTargetTestClassMultifields)
        .ThenInclude(t => t.FmMultiField)
    .Include(i => i.FmTargetTestClassMultifields)
        .ThenInclude(t => t.FmMultiFieldValue)
    .FirstOrDefault(f => f.Id == fmTargetTestClass.Id);

Assert.IsNotNull(fmTargetTestClassWithIncludes);
Assert.AreEqual(6, fmTargetTestClassWithIncludes.FmTargetTestClassMultifields.Count);

FmTargetTestClassDto testDto = new();
CacheFmMultiFieldMapper.MapToDtoDictionary(fmTargetTestClassWithIncludes.FmTargetTestClassMultifields,
    testDto.FmTargetTestClassMultifields);

Assert.AreEqual(dto.FmTargetTestClassMultifields.Count, testDto.FmTargetTestClassMultifields.Count);
```

All samples are available in the test project located at [`.src/FMMultifieldMapperTests`](./src/FMMultifieldMapperTests).

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request. Make sure to follow the coding standards and include tests for any new features or bug fixes.

