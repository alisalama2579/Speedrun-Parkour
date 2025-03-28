using UnityEngine;

public class TraversableTerrain : MonoBehaviour
{
    public TraversableTerrainStats stats;
    protected SpriteRenderer sprite;
    protected virtual void Awake() { sprite = GetComponent<SpriteRenderer>(); }
    public virtual void OnEnterTerrain() { }

    protected virtual void OnDisable() { }
}

