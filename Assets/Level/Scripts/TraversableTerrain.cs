using UnityEngine;

public class TraversableTerrain : MonoBehaviour
{
    public TraversableTerrainStats stats;
    protected SpriteRenderer sprite;
    protected virtual void Start() { sprite = GetComponent<SpriteRenderer>();}
    public virtual void OnPlayerEnterTerrain(LandMovement controller) { }

    protected virtual void OnDisable() { }
}
