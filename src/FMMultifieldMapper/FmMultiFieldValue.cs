namespace FMMultiFieldMapper;

/// <summary>
/// FmMultiFieldValue
/// </summary>
public class FmMultiFieldValue
{
    /// <summary>
    /// Index
    /// </summary>
    public int FmMultiFieldValueId { get; set; }
    /// <summary>
    /// Value
    /// </summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>
    /// Order - start with 0
    /// </summary>
    public int Order { get; set; }
    /// <summary>
    /// FmMultiFieldId
    /// </summary>
    public int FmMultiFieldId { get; set; }
    /// <summary>
    /// FmMultiField
    /// </summary>
    public FmMultiField? FmMultiField { get; set; }
}