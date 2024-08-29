using FMMultiFieldMapper;

namespace FMMultifieldMapperTests;

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
