using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "PlayerBurrowMovementStats", menuName = "PlayerBurrowMovementStats")]
public class PlayerBurrowMovementStats : ScriptableObject
{
    public LayerMask collisionLayerMask;
    public float collisionDetectionDistance;
    public float speed;
    public float rotationSpeed;

    public float bounceVel;
    public float bounceDuration;
    public float bounceDeceleration;
    [Range(0, 1)] public float bounceNormalBias;
    public readonly float BOUNCE_COOL_DOWN = 0.1f;

    public AnimationCurve dashSpeedCurve;
    public float dashVel;
    public AnimationCurve dashControlCurve;
    public float dashControlMult;
    public float dashDuration;

}
