namespace FMMultifieldMapper;

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
    /// Optional order if DataMember Attribute is not set
    /// </summary>
    public int Order { get; set; }
}
