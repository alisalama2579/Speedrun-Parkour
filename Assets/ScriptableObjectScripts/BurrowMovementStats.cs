using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "PlayerBurrowMovementStats", menuName = "PlayerBurrowMovementStats")]
public class BurrowMovementStats : ScriptableObject
{
    public float exitDetectionDistance;
    public float collisionDetectionDistance;

    [Space(5)]
    [Header("Input Move")]
    public float acceleration;
    public float maxSpeed;
    public float rotationSpeed;
    public float oppositeTurnControl;

    [Space(5)]
    [Header("Bounce")]
    #region Bounce
    [Range(1, 100)]
    public int bounceCheckFrequency;
    public float bounceDuration;
    public float bounceSpeed;
    public AnimationCurve bounceSpeedCurve;
    public AnimationCurve bounceControlCurve;
    public float bounceMoveSpeedMult;

    [Range(0, 1)] public float bounceNormalBias;
    public readonly float BOUNCE_COOL_DOWN = 0.1f;
    #endregion

    [Space(5)]
    [Header("Dash")]
    #region Dash
    [Range(0, 1)] public float progressToDashChain;
    public float directEntryMultiplier;
    public float dashBuffer;
    public float dashDuration;
    public float dashSpeed;

    public AnimationCurve dashSpeedCurve;
    public AnimationCurve dashControlCurve;
    public float dashControlMult;

    #endregion
}
