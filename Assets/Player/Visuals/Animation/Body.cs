using UnityEngine;
using static PlayerAnimator;
using static UnityEngine.Mathf;

public class Body : Animation
{
    private int wallEntryDir = 1;

    public override void InitializeAnimation(PlayerAnimatorStats stats, PlayerAnimator animator)
    {
        this.stats = stats;
        this.animator = animator;
    }

    public override void UpdateAnimation() { }

    public override void HandleGround()
    {
        ApplyBodyTilt(stats.maxBodyTilt, stats.tiltSpeed);
    }

    public override void HandleAir()
    {
        ApplyBodyTilt(stats.maxAirBodyTilt, stats.tiltSpeed);
    }


    public override void HandleWall()
    {
        ApplyBodyTilt(-stats.wallTilt, stats.wallTiltSpeed);
    }

    public override void HandleStateTransition(PlayerAnimationState state)
    {
        switch (state)
        {
            case PlayerAnimationState.OnWall:
                wallEntryDir = (int)animator.animationFrameValues.moveInput;
                HandleWall();
                break;
            case PlayerAnimationState.InAir: HandleAir(); break;
            case PlayerAnimationState.OnGround: HandleGround(); break;
        }
    }

    private void ApplyBodyTilt(float bodyTilt, float tiltSpeed)
    {
        int sign = animator.animationFrameValues.isOnWall ? wallEntryDir : (int)Sign(animator.animationFrameValues.moveInput);
        float tilt = MoveTowardsAngle(transform.eulerAngles.z, Lerp(0, bodyTilt, animator.xSpeed), tiltSpeed * Time.deltaTime);

        float yRotation = sign == 1 ? 0 : 180;
        transform.localEulerAngles = new Vector3(transform.eulerAngles.x, yRotation, tilt);
    }
}
