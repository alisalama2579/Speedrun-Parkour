using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "PlayerLandControllerStats", menuName = "PlayerLandControllerStats")]
public class PlayerLandControllerStats : ScriptableObject
{
    #region Horizontal

    [Header("Horizontal acceleration, speed and momentum")]
    [Range(1, 100)] public float maxLandSpeed;
    [Range(1, 100)] public float minLandSpeed;
    [Range(1, 100)] public float minAcceleration;
    [Range(1, 200)] public float maxAcceleration;
    [Range(1, 100)] public float minDeceleration;
    [Range(1, 200)] public float maxDeceleration;
    public float turningAccelerationMultiplier;

    [Range(0, 5)] public float airAccelerationMultiplier;
    [Range(0, 5)] public float airSpeedMultiplier;
    public float apexSpeedIncrease;

    #endregion

    [Space(5)]

    #region Momentum

    [Header("Momentum")]

    public float wallJumpMomentumIncrease;
    public float jumpMomentumIncrease;
    [Range(0, 1)] public float momentumGainSpeed;
    [Range(0, 1)] public float momentumLossSpeed;
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

    public LayerMask collisionLayerMask;
    public float jumpVelocity;
    public float horizontalJumpVelocityDeceleration;
    public float jumpApexRange;
    public float gravAfterFalloffMultiplier;

    #endregion

    [Space(5)]
    #region Dash

    [Header("Dash")]

    public Vector2 targetDashVelocity;
    public float dashDuration;
    public float dashInputUpMultiplier;

    public float dashOpposingMovementFriction;

    public AnimationCurve dashSpeedCurve;
    public AnimationCurve dashHorizontalCurve;
    public AnimationCurve dashVerticalCurve;

    #endregion

    [Space(5)]

    #region Grav and down velocity

    [Header("Gravity and down velocity")]
    public float maxDownVelocity;
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
    public float maxYVelocityForWallGrab;

    public float onWallGravity;
    public float wallJumpVelocity;
    [Range(0, 1)] public float wallJumpUpBias;

    #endregion

    [Space(5)]

    #region Miscalleneous
    [Header("Miscalleneous")]
    public float slipStrength;
    #endregion
}
