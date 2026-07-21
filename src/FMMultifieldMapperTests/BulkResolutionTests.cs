using FMMultiFieldMapper;

namespace FMMultifieldMapperTests;

[TestClass]
public sealed class BulkResolutionTests
{
    [TestMethod]
    public async Task DefaultBulkResolvers_DeduplicateSingularCalls()
    {
        var mapper = new FallbackOnlyMapper();
        var source = CreateSourceWithRepeatedValue();
        var target = new List<FmTargetTestClassMultifield>();

        await mapper.Map(source, target);

        Assert.AreEqual(1, mapper.MultiFieldCalls);
        Assert.AreEqual(2, mapper.MultiFieldValueCalls);
    }

    [TestMethod]
    public async Task Map_InvokesEachBulkResolverOnceWithDistinctRequests()
    {
        var mapper = new RecordingBulkMapper();
        var source = CreateSourceWithRepeatedValue();
        var target = new List<FmTargetTestClassMultifield>();

        await mapper.Map(source, target);

        Assert.AreEqual(1, mapper.MultiFieldBatchCalls);
        Assert.AreEqual(1, mapper.MultiFieldNameRequestCount);
        Assert.AreEqual(1, mapper.MultiFieldValueBatchCalls);
        Assert.AreEqual(2, mapper.MultiFieldValueRequestCount);
    }

    [TestMethod]
    public async Task Map_RejectsIncompleteMultiFieldBatchResult()
    {
        var mapper = new IncompleteBulkMapper(omitName: true);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => mapper.Map(CreateSourceWithRepeatedValue(), new List<FmTargetTestClassMultifield>()));

        StringAssert.Contains(exception.Message, "did not return an ID for name 'Themen'");
    }

    [TestMethod]
    public async Task Map_RejectsIncompleteMultiFieldValueBatchResult()
    {
        var mapper = new IncompleteBulkMapper(omitName: false);

        var exception = await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => mapper.Map(CreateSourceWithRepeatedValue(), new List<FmTargetTestClassMultifield>()));

        StringAssert.Contains(exception.Message, "bulk multi-field value resolver did not return an ID");
    }

    private static FmSourceTestClass CreateSourceWithRepeatedValue()
    {
        return new FmSourceTestClass
        {
            Themen1 = "Repeated",
            Themen2 = "Repeated",
            Themen3 = "Distinct"
        };
    }

    private class FallbackOnlyMapper : FmMultiFieldMap
    {
        private int nextId;

        public int MultiFieldCalls { get; private set; }

        public int MultiFieldValueCalls { get; private set; }

        public override Task<int> GetOrCreateMultiFieldId(string name)
        {
            MultiFieldCalls++;
            return Task.FromResult(++nextId);
        }

        public override Task<int> GetOrCreateMultiFieldValueId(int multifieldId, string value)
        {
            MultiFieldValueCalls++;
            return Task.FromResult(++nextId);
        }
    }

    private sealed class RecordingBulkMapper : FallbackOnlyMapper
    {
        public int MultiFieldBatchCalls { get; private set; }

        public int MultiFieldNameRequestCount { get; private set; }

        public int MultiFieldValueBatchCalls { get; private set; }

        public int MultiFieldValueRequestCount { get; private set; }

        protected override async Task<IReadOnlyDictionary<string, int>> GetOrCreateMultiFieldIds(
            IReadOnlyCollection<string> names)
        {
            MultiFieldBatchCalls++;
            MultiFieldNameRequestCount = names.Count;
            return await base.GetOrCreateMultiFieldIds(names);
        }

        protected override async Task<IReadOnlyDictionary<(int MultiFieldId, string Value), int>>
            GetOrCreateMultiFieldValueIds(IReadOnlyCollection<(int MultiFieldId, string Value)> values)
        {
            MultiFieldValueBatchCalls++;
            MultiFieldValueRequestCount = values.Count;
            return await base.GetOrCreateMultiFieldValueIds(values);
        }
    }

    private sealed class IncompleteBulkMapper(bool omitName) : FallbackOnlyMapper
    {
        protected override Task<IReadOnlyDictionary<string, int>> GetOrCreateMultiFieldIds(
            IReadOnlyCollection<string> names)
        {
            return omitName
                ? Task.FromResult<IReadOnlyDictionary<string, int>>(new Dictionary<string, int>())
                : base.GetOrCreateMultiFieldIds(names);
        }

        protected override Task<IReadOnlyDictionary<(int MultiFieldId, string Value), int>>
            GetOrCreateMultiFieldValueIds(IReadOnlyCollection<(int MultiFieldId, string Value)> values)
        {
            return Task.FromResult<IReadOnlyDictionary<(int MultiFieldId, string Value), int>>(
                new Dictionary<(int MultiFieldId, string Value), int>());
        }
    }
}
