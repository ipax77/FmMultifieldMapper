namespace FMMultifieldMapper;

/// <summary>
/// FmMultifieldValue
/// </summary>
public class FmMultifieldValue
{
    /// <summary>
    /// Index
    /// </summary>
    public int FmMultifieldValueId { get; set; }
    /// <summary>
    /// Value
    /// </summary>
    public string Value { get; set; } = string.Empty;
    /// <summary>
    /// Order
    /// </summary>
    public int Order { get; set; }
    /// <summary>
    /// FmMultifieldId
    /// </summary>
    public int FmMultifieldId { get; set; }
    /// <summary>
    /// FmMultifield
    /// </summary>
    public FmMultifield? FmMultifield { get; set; }
}