using UnityEngine;
using static PlayerAnimator;
using static UnityEngine.Mathf;

public class Legs : Animation
{
    public override void InitializeAnimation(PlayerAnimatorStats stats, PlayerAnimator animator)
    {
        this.stats = stats;
        this.animator = animator;
    }

    [SerializeField] protected Transform legLeft, legRight;

    private float animationProgress;
    private Vector2 targetLegAngles;

    public override void UpdateAnimation()
    {
    }

    public override void HandleGround()
    {
        animationProgress += (animator.xSpeed * Time.deltaTime) / stats.walkCycleDuration;
        if (animator.xSpeed < 0.2f) animationProgress = Lerp(animationProgress, 0, animator.xSpeed / 0.2f);

        float targetAngle = Sin(PI * animationProgress) * stats.legRotationRange + stats.legRotationRange * 0.5f;

        targetLegAngles.x = targetAngle + stats.legRestingRotation;
        targetLegAngles.y = -targetLegAngles.x;

        ApplyLegRotation();
    }

    public override void HandleAir()
    {

        if (animator.animationFrameValues.velocity.y > 0) targetLegAngles.x = stats.legRisingRotation * (1 + animator.ySpeed);
        else  targetLegAngles.x = stats.legFallingRotation * (1 + animator.ySpeed);
        
        targetLegAngles.y = -targetLegAngles.x;
        ApplySmoothLegRotation();
    }


    public override void HandleWall()
    {
        targetLegAngles.x = stats.legWallRotation;
        targetLegAngles.y = targetLegAngles.x;

        ApplySmoothLegRotation();
    }

    private void ApplyLegRotation()
    {
        legLeft.transform.localEulerAngles = Vector3.forward * targetLegAngles.x;
        legRight.transform.localEulerAngles = Vector3.forward * targetLegAngles.y;
    }

    private float smoothVelocity;
    private void ApplySmoothLegRotation()
    {
        legLeft.transform.localEulerAngles = Vector3.forward * MoveTowardsAngle(legLeft.transform.localEulerAngles.z, targetLegAngles.x, stats.smoothSpeed * Time.deltaTime);
        legRight.transform.localEulerAngles = Vector3.forward * MoveTowardsAngle(legRight.transform.localEulerAngles.z, targetLegAngles.y, stats.smoothSpeed * Time.deltaTime);
    }

    public override void HandleStateTransition(PlayerAnimationState state)
    {
        switch (state)
        {
            case PlayerAnimationState.OnWall: HandleWall(); break;
            case PlayerAnimationState.InAir: HandleAir();  break;

            case PlayerAnimationState.OnGround: HandleGround();

                if (animator.animationState == PlayerAnimationState.InAir) animationProgress = animator.xSpeed * Time.deltaTime /stats.walkCycleDuration;
                 break;
        }
    }
}
