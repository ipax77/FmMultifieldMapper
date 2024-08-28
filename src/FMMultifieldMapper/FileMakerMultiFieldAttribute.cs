namespace FMMultiFieldMapper;

/// <summary>
/// Describes a FileMakerMultiField
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public sealed class FileMakerMultiFieldAttribute : Attribute
{
    /// <summary>
    /// Map target name
    /// </summary>
    public string MultiFieldName { get; set; } = string.Empty;
    /// <summary>
    /// Order - start with 0
    /// </summary>
    public int Order { get; set; }
}
