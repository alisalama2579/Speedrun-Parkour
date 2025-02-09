using UnityEngine;

[CreateAssetMenu(fileName = "PlayerLandControllerStats", menuName = "PlayerControllerStats")]
public class PlayerLandControllerStats : ScriptableObject
{
    [Header("Horizontal acceleration, speed and momentum")]
    [Range(1, 100)] public float maxLandSpeed;
    [Range(1, 100)] public float minLandSpeed;
    [Range(1, 100)] public float minAcceleration;
    [Range(1, 200)] public float maxAcceleration;

    [Range(0, 1)] public float momentumGainSpeed;
    [Range(0, 1)] public float momentumLossSpeed;
    public float momentumGainThreshold;

    [Range(0, 5)] public float airAccelerationMultiplier;
    [Range(0, 5)] public float airSpeedMultiplier;
    public float apexSpeedMultiplier;

    [Space(5)]

    [Header("Jump")]
    public LayerMask groundLayerMask;
    public float jumpHeight;
    [Range(0, 1)] public float jumpUpBias;

    [Range(0, 1)] public float coyoteTime;
    [Range(0, 1)] public float jumpBuffer;

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
    public float wallDetectionDistance;
}
