using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "AnimationStatsHolder", menuName = "ScriptableObjects/Animation/AnimationStatsHolder")]
public class AnimationStatsHolder : ScriptableObject
{
    public float minSpeedForRun;
    public float minSpeedForWalk;

    public float rollAnimationTime;
    public float rollRotationSpeed;

    public float burrowDashTime;
}
