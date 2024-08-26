namespace FMMultifieldMapper;

/// <summary>
/// IFmTargetMultifield
/// </summary>
public interface IFmTargetMultifield
{
    /// <summary>
    /// FmMultifieldId
    /// </summary>
    public int FmMultifieldId { get; set; }
    /// <summary>
    /// FmMultiField
    /// </summary>
    public FmMultifield? FmMultiField { get; set; }
    /// <summary>
    /// FmMultifieldValueId
    /// </summary>
    public int FmMultifieldValueId { get; set; }
    /// <summary>
    /// FmMultiFieldValue
    /// </summary>
    public FmMultifieldValue? FmMultiFieldValue { get; set; }
}