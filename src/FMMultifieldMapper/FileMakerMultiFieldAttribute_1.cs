namespace FMMultiFieldMapper;

/// <summary>
/// Describes a FileMakerMultifield
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class FileMakerMultifieldAttribute : Attribute
{
    /// <summary>
    /// Map target name
    /// </summary>
    public string MultifieldName { get; set; } = string.Empty;
    /// <summary>
    /// Order - start with 0
    /// </summary>
    public int Order { get; set; }
}
