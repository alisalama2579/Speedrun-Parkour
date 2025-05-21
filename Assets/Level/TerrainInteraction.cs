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
