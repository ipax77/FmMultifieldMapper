namespace FMMultifieldMapperTests;

public class FmTargetTestClass
{
    public int Id { get; set; }
    public ICollection<FmTargetTestClassMultifield> FmTargetTestClassMultifields { get; set; } = [];
}
