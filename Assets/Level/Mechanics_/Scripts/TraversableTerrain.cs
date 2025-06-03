using UnityEngine;

public class TraversableTerrain : MonoBehaviour
{
    public enum SurfaceSoundType
    {
        Stone,
        Snow,
        Ice,
        WetStone
    }
    public enum TerrainType
    {
        Ground,
        Wall
    }

    public SurfaceSoundType soundType;
    public LevelMechanicStats stats;
    protected SpriteRenderer sprite;
    protected virtual void Awake() { sprite = GetComponent<SpriteRenderer>(); }

    protected virtual void OnDisable() { }
}


