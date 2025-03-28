using UnityEngine;
using static UnityEngine.Mathf;


/// <summary>
///  Temporary, will be replaced later with a sprite-based animation system
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    #region AnimationStatesAndValues
    public enum PlayerAnimationState
    {
        OnGround,
        InAir,
        OnWall
    }


    public struct AnimationValues
    {
        public float moveInput;
        public Vector2 velocity;
        public bool isGrounded;
        public bool isOnWall;
    }

    [HideInInspector] public AnimationValues animationFrameValues;
    [HideInInspector] public PlayerAnimationState animationState;

    #endregion

    [SerializeField] private PlayerAnimatorStats stats;
    [SerializeField] public Animation[] animations;

    public float xSpeed { get; private set; }
    public float ySpeed { get; private set; }

    public void InitializeAnimator() 
    {
        for (int i = 0; i < animations.Length; i++)
        {
            animations[i].InitializeAnimation(stats, this);
        }
    }

    private float lastMoveInput;
    public void UpdateAnimator(AnimationValues animationValues)
    {
        animationFrameValues = animationValues;

        if (animationFrameValues.moveInput == 0) animationFrameValues.moveInput = lastMoveInput;
        lastMoveInput = animationFrameValues.moveInput;

        xSpeed = Clamp01(Abs(animationFrameValues.velocity.x) / stats.maxPlayerAnimationSpeed);
        ySpeed = Clamp01(Abs(animationFrameValues.velocity.y) / stats.maxPlayerAnimationSpeed);

        for (int i = 0; i < animations.Length; i++) animations[i].UpdateAnimation();

        PlayerAnimationState newAnimationState;
        if (animationFrameValues.isGrounded) newAnimationState = PlayerAnimationState.OnGround;
        else if (animationFrameValues.isOnWall) newAnimationState = PlayerAnimationState.OnWall;
        else newAnimationState = PlayerAnimationState.InAir;

        if (animationState != newAnimationState)
        {
            for (int i = 0; i < animations.Length; i++) animations[i].HandleStateTransition(newAnimationState);
            animationState = newAnimationState;
            return;
        }

        animationState = newAnimationState;
        switch (animationState)
        {
            case PlayerAnimationState.OnWall:
                for (int i = 0; i < animations.Length; i++) animations[i].HandleWall();
                break;   
            
            case PlayerAnimationState.InAir:
                for (int i = 0; i < animations.Length; i++) animations[i].HandleAir();
                break;

            case PlayerAnimationState.OnGround:
                for (int i = 0; i < animations.Length; i++) animations[i].HandleGround();
                break;
        }
    }
}
