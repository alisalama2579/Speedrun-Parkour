using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "PlayerBurrowMovementStats", menuName = "PlayerBurrowMovementStats")]
public class BurrowMovementStats : ScriptableObject
{
    public float collisionDetectionDistance;
    [FormerlySerializedAs("speed")]
    public float acceleration;
    [FormerlySerializedAs("maxVelocity")]
    public float maxSpeed;
    public float rotationSpeed;
    [FormerlySerializedAs("turnSensitivity")]
    public float oppositeTurnControl;

    public float bounceDuration;
    [FormerlySerializedAs("bounceVel")]
    public float bounceSpeed;
    public AnimationCurve bounceSpeedCurve;
    public AnimationCurve bounceControlCurve;
    [FormerlySerializedAs("bounceControlMult")]
    public float bounceMoveSpeedMult;

    [Range(0, 1)] public float bounceNormalBias;
    public readonly float BOUNCE_COOL_DOWN = 0.1f;

    public AnimationCurve dashSpeedCurve;
    [FormerlySerializedAs("dashVel")]
    public float dashSpeed;
    public AnimationCurve dashControlCurve;
    public float dashControlMult;
    public float dashDuration;

}
