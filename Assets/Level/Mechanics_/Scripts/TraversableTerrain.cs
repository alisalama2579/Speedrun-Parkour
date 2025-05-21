using UnityEngine;

public class TraversableTerrain : MonoBehaviour
{
    public enum TerrainType
    {
        Ground,
        Wall
    }

    public LevelMechanicStats stats;
    protected SpriteRenderer sprite;
    protected virtual void Awake() { sprite = GetComponent<SpriteRenderer>(); }

    protected virtual void OnDisable() { }
}


