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
    public float slipMultiplier;

    [Range(0, 5)] public float airAccelerationMultiplier;
    [Range(0, 5)] public float airSpeedMultiplier;
    public float apexSpeedIncrease;




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
    public float horizontalJumpVelocityDeceleration;
    [Range(0, 1)] public float coyoteTime;
    public float jumpBuffer;
    public float jumpApexRange;
    public float jumpVelocityFallof;
    public float gravAfterFalloffMultiplier;





    [Space(5)]

    [Header("Momentum")]
    public Vector2 targetDashVelocity;
    public float dashDuration;

    public AnimationCurve dashSpeedCurve;
    public AnimationCurve dashHorizontalCurve;
    public AnimationCurve dashVerticalCurve;


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
    public string wallTag;
    public float wallDetectionDistance;
    public float wallNormalRange;

    public float timeToFullSpeedFromWall;
    public float maxYVelocityForWallGrab;

    public float onWallGravity;
    public float wallJumpForce;
    [Range(0, 1)] public float wallJumpUpBias;
}
