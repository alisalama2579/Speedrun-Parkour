using UnityEngine;
using static PlayerAnimator;

public abstract class Animation : MonoBehaviour
{
    protected PlayerAnimatorStats stats;
    protected PlayerAnimator animator;

    #region ComponentAnimation

    public abstract void InitializeAnimation(PlayerAnimatorStats stats, PlayerAnimator animator);

    public abstract void UpdateAnimation();

    public abstract void HandleStateTransition(PlayerAnimationState newState);

    public abstract void HandleAir();

    public abstract void HandleGround();

    public abstract void HandleWall();

    #endregion
}
