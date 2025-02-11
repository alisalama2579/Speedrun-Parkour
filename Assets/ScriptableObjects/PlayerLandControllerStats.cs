using UnityEngine;

[CreateAssetMenu(fileName = "PlayerLandControllerStats", menuName = "PlayerControllerStats")]
public class PlayerLandControllerStats : ScriptableObject
{
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
    public float apexSpeedMultiplier;

    [Space(5)]

    [Header("Momentum")]
    public float wallJumpMomentumIncrease;
    public float jumpMomentumIncrease;
    [Range(0, 1)] public float momentumGainSpeed;
    [Range(0, 1)] public float momentumLossSpeed;
    public float momentumGainThreshold;

    [Space(5)]

    [Header("Jump")]
    public LayerMask collisionLayerMask;
    public float jumpForce;
    [Range(0, 1)] public float jumpUpBias;

    [Range(0, 1)] public float coyoteTime;
    public float jumpBuffer;

    public float jumpApexRange;
    public float jumpVelocityFallof;
    public float gravAfterFalloffMultiplier;

    [Space(5)]

    [Header("Gravity and down velocity")]
    public float maxDownVelocity;
    public float gravity;
    public float apexAntigravity;

    [Space(5)]

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

    [Header("Wall handling")]
    public float timeToWallGrab;
    public float wallDetectionDistance;
    public float onWallGrvaity;
    public float wallFrictionAcceleration;
    public float wallJumpForce;
    public float wallNormalRange;
    [Range(0, 1)] public float wallJumpUpBias;
}
