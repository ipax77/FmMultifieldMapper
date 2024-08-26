using FMMultifieldMapper;

namespace FMMultifieldMapperTests;

public class FmTargetTestClassMultifield : IFmTargetMultifield
{
    public int FmTargetTestClassMultifieldId { get; set; }
    public int FmMultifieldId { get; set; }
    public FmMultifield? FmMultiField { get; set; }
    public int FmMultifieldValueId { get; set; }
    public FmMultifieldValue? FmMultiFieldValue { get; set; }
    public int FmTargetTestClassId { get; set; }
    public FmTargetTestClass? FmTargetTestClass { get; set; }
}
