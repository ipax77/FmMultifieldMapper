namespace FMMultiFieldMapper;

/// <summary>
/// FmMultifieldValue
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
    /// FmMultifieldId
    /// </summary>
    public int FmMultiFieldId { get; set; }
    /// <summary>
    /// FmMultifield
    /// </summary>
    public FmMultiField? FmMultifield { get; set; }
}