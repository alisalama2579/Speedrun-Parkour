using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "PlayerLandControllerStats", menuName = "PlayerLandControllerStats")]
public class LandMovementStats : ScriptableObject
{
    public GameObject sandBurrowVisualiser;

    public float burrowDetectionDistance;
    public float sandDetectionDistance;
    public float sandOvershoot;

    public float launchOpposingMovementFriction;
    public float maxLaunchMoveSpeed;
    public float launchDuration;
    public Vector2 launchVel;
    public float weakLaunchVel;
    public AnimationCurve launchSpeedCurve;
    public AnimationCurve launchGravCurve;
    public AnimationCurve launchControlCurve;


    #region Horizontal

    [Header("Horizontal acceleration, speed and momentum")]
    [Range(1, 100)] public float maxLandSpeed;
    [Range(1, 100)] public float minLandSpeed;
    [Range(1, 100)] public float minAcceleration;
    [Range(1, 200)] public float maxAcceleration;
    [Range(1, 100)] public float minDeceleration;
    [Range(1, 200)] public float maxDeceleration;
    public float turningAccelerationMultiplier;
    public float additionalHorizontalMovementDeceleration;

    [Range(0, 5)] public float airAccelerationMultiplier;
    [Range(0, 5)] public float airSpeedMultiplier;
    public float apexSpeedIncrease;

    #endregion

    [Space(5)]

    #region Momentum

    [Header("Momentum")]

    public float wallJumpMomentumIncrease;
    public float jumpMomentumIncrease;
    public float hurtMomentumLoss;
    [Range(0, 1)] public float momentumGainSpeed;
    [Range(0, 1)] public float momentumLossSpeed;
    public float startingMomentum;
    public float momentumGainThreshold;

    #endregion

    [Space(5)]

    #region Jump

    [Header("Jump")]

    [Tooltip("Gravity reduction when at the apex of a jump")] public float apexAntigravity;
    [Tooltip("jump velocity after which fallof gravity is applied")] public float jumpVelocityFallof;
    [Range(0, 1)] public float jumpUpBias;
    [Range(0, 1)] public float coyoteTime;
    [Range(0, 1)] public float jumpBuffer;

    public float jumpVelocity;
    public float jumpApexRange;
    public float gravAfterFalloffMultiplier;

    #endregion

    [Space(5)]
    #region Dash

    [Header("Dash")]

    public Vector2 horizontalDashVel;
    public Vector2 diagonalDashVel;
    public float dashDuration;
    public float dashOpposingMovementFriction;

    public AnimationCurve dashSpeedCurve;
    public AnimationCurve dashHorizontalVelMult;
    public AnimationCurve dashGravMult;

    #endregion

    [Space(5)]

    #region Grav and down velocity

    [Header("Gravity and down velocity")]
    [FormerlySerializedAs("maxDownVelocity")]
    public float maxDownVel;
    public float gravity;

    #endregion

    [Space(5)]

    #region Ground/ledge detect

    [Header("Ground and wall detection")]
    public float ceilingHitPush;
    public float groundingPush;

    public float maxCeilNormal;
    [Range(0.1f, 1)] public float minGroundNormal;

    public float skinWidth;
    public float groundedDistance;
    public float groundSnapDistance;
    public float ceilingDistance;
    public float ledgeGrabDistance;

    #endregion

    [Space(5)]
    #region Wall

    [Header("Wall handling")]
    public string wallTag;
    public float wallDetectionDistance;
    public float wallNormalRange;

    public float timeToFullSpeedFromWall;
    [FormerlySerializedAs("maxYVelocityForWallGrab")]
    public float maxYVelForWallGrab;

    public float onWallGravity;
    [FormerlySerializedAs("wallJumpVelocity")]
    public float wallJumpVel;
    [Range(0, 1)] public float wallJumpUpBias;

    #endregion

    [Space(5)]

    #region LevelMechanics
    [Header("Level Mechanics")]
    public float slipStrength;
    public Vector2 slowAreaDrag;
    public float slowAreaMomentumLoss;
    #endregion
}
