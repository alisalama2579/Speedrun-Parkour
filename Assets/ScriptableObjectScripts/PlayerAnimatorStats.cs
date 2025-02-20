using UnityEngine;

[CreateAssetMenu(fileName = "PlayerAnimatorStats", menuName = "PlayerAnimatorStats")]
public class PlayerAnimatorStats : ScriptableObject
{
    public float maxPlayerAnimationSpeed;

    [Header("Legs")]

    public float legRotationRange;
    public float walkCycleDuration;
    public float legRestingRotation;
    public float legWallRotation;
    public float legRisingRotation;
    public float legFallingRotation;
    public float smoothSpeed;



    [Space(5)]

    [Header("Body")]

    public float maxBodyTilt;
    public float maxAirBodyTilt;
    public float tiltSpeed;

    public float wallTilt;
    public float wallTiltSpeed;
}
