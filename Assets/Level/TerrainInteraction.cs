using System;

[Serializable]
public class Surface
{
    public TraversableTerrain terrain;
    public TerrainSurfaceType surfaceType;
    public Surface(TraversableTerrain terrain, TerrainSurfaceType surfaceType)
    {
        this.terrain = terrain;
        this.surfaceType = surfaceType;
    }

    public override bool Equals(object obj)
    {
        return obj is Surface surface && this == surface;
    }
    public static bool operator ==(Surface lhs, Surface rhs)
    {
        bool leftNull = lhs is null;
        bool rightNull = rhs is null;
        if (leftNull || rightNull)
            return leftNull == rightNull;

        return lhs.terrain == rhs.terrain &&
               lhs.surfaceType == rhs.surfaceType;
    }
    public static bool operator !=(Surface lhs, Surface rhs) => !(lhs == rhs);
}
public enum TerrainSurfaceType
{
    Wall,
    Ground,
    Ceiling
}

public interface ITerrainInteraction
{
    public CollisionType CollisionType { get; set; }
    public TerrainSurfaceType SurfaceType { get; }
}

public class TerrainTouch : ITerrainInteraction
{
    public CollisionType CollisionType { get; set; }
    public TerrainSurfaceType SurfaceType { get; }
    public TerrainTouch(CollisionType type, TerrainSurfaceType surfaceType)
    {
        CollisionType = type;
        SurfaceType = surfaceType;
    }
}
public class TerrainInteract : ITerrainInteraction
{
    public CollisionType CollisionType { get; set; }
    public TerrainSurfaceType SurfaceType { get; }
    public TerrainInteract(CollisionType type, TerrainSurfaceType surfaceType)
    {
        CollisionType = type;
        SurfaceType = surfaceType;
    }
}
