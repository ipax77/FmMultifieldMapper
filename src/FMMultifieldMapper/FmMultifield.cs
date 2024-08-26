namespace FMMultifieldMapper;

/// <summary>
/// FmMultifield
/// </summary>
public class FmMultifield
{
    /// <summary>
    /// Index
    /// </summary>
    public int FmMultifieldId { get; set; }
    /// <summary>
    /// Name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// FmMultifield Values
    /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
    public ICollection<FmMultifieldValue> Values { get; set; } = [];
#pragma warning restore CA2227 // Collection properties should be read only
}
