using System.Numerics;
using Friflo.Engine.ECS;

public struct Velocity : IComponent { public Vector3 value; };

// Example struct Position
public static class Helpers
{
    // Implicit cast from Position to Vector3
    public static Vector3 ToVec3(Position p) => new Vector3(p.x, p.y, p.z);

    // Implicit cast from Vector3 to Position
    // public static implicit operator Position(Vector3 v) => new Position { value = v };
}