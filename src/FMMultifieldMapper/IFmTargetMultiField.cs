namespace FMMultiFieldMapper;

/// <summary>
/// IFmTargetMultiField
/// </summary>
public interface IFmTargetMultiField
{
    /// <summary>
    /// FmMultiFieldId
    /// </summary>
    public int FmMultiFieldId { get; set; }
    /// <summary>
    /// FmMultiField
    /// </summary>
    public FmMultiField? FmMultiField { get; set; }
    /// <summary>
    /// FmMultiFieldValueId
    /// </summary>
    public int FmMultiFieldValueId { get; set; }
    /// <summary>
    /// FmMultiFieldValue
    /// </summary>
    public FmMultiFieldValue? FmMultiFieldValue { get; set; }
    /// <summary>
    /// Order - start with 0
    /// </summary>
    public int Order { get; set; }
}

/// <summary>
/// IFmTargetMultiFieldDto
/// </summary>
public interface IFmTargetMultiFieldDto
{
    /// <summary>
    /// FmMultiFields
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    public Dictionary<string, List<string>> FmMultiFields { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
}