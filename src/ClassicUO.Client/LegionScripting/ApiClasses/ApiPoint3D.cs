namespace ClassicUO.LegionScripting.ApiClasses;

/// <summary>
/// Represents a point in a three-dimensional space
/// </summary>
public class ApiPoint3D
{
    public int X { get; init; }
    public int Y { get; init; }
    public int Z { get; init; }

    public override string ToString() => $"({X}, {Y}, {Z})";
}
