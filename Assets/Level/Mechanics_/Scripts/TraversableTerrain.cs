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
    public virtual void OnEnterTerrain() { }
    public virtual void OnCollideWithTerrain(LandMovement.TerrainInteractionType interactionType) { }

    protected virtual void OnDisable() { }
}

