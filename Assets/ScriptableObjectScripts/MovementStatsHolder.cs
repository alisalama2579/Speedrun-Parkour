using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "MovementStatsHolder", menuName = "MovementStatsHolder")]
public class MovementStatsHolder : ScriptableObject
{
    public LayerMask terrainLayerMask;
    public LayerMask sandLayerMask;
    public LayerMask collisionLayerMask;

    public LandMovementStats landStats;
    public BurrowMovementStats burrowStats;
    public SandEntryMovementStats interStateDashStats;

}
