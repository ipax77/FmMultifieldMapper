namespace FMMultifieldMapperTests;

public record FmTargetTestClassDto
{
    public Dictionary<string, List<string>> FmTargetTestClassMultifields { get; set; } = [];
}