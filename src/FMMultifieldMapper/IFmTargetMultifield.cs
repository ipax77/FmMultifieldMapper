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

/// <summary>
/// IFmTargetMultifieldDto
/// </summary>
public interface IFmTargetMultifieldDto
{
    /// <summary>
    /// FmMultifields
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    public Dictionary<string, List<string>> FmMultifields { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}