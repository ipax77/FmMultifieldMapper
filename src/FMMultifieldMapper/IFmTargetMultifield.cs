namespace FMMultifieldMapper;

/// <summary>
/// IFmTargetMultifield
/// </summary>
public interface IFmTargetMultiField
{
    /// <summary>
    /// FmMultifieldId
    /// </summary>
    public int FmMultiFieldId { get; set; }
    /// <summary>
    /// FmMultiField
    /// </summary>
    public FmMultiField? FmMultiField { get; set; }
    /// <summary>
    /// FmMultifieldValueId
    /// </summary>
    public int FmMultiFieldValueId { get; set; }
    /// <summary>
    /// FmMultiFieldValue
    /// </summary>
    public FmMultiFieldValue? FmMultiFieldValue { get; set; }
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