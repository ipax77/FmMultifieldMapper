namespace FMMultiFieldMapper;

/// <summary>
/// FmMultiField
/// </summary>
public class FmMultiField
{
    /// <summary>
    /// Index
    /// </summary>
    public int FmMultiFieldId { get; set; }
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// FmMultiField Values
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    public ICollection<FmMultiFieldValue> Values { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only
}
