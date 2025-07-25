using UnityEngine;

[CreateAssetMenu(fileName = "InterStateDashMovementStats", menuName = "ScriptableObjects/Player/InterStateDashMovementStats")]
public class SandEntryMovementStats : ScriptableObject
{
    public float entrySpeed;
    public float velToSpeedRatio;
}
