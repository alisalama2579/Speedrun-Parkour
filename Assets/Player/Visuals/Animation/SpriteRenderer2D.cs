using UnityEngine;

public class SpriteRenderer2D : MonoBehaviour
{
    //[Header("References")]
    //[SerializeField]
    //private Animator animator;
    //[SerializeField] private SpriteRenderer sprite;

    //[Header("Settings")]
    //[SerializeField, Range(1f, 3f)]
    //private float maxIdleSpeed = 2;

    //[SerializeField] private float _maxTilt = 5;
    //[SerializeField] private float _tiltSpeed = 20;

    //[Header("Particles")][SerializeField] private ParticleSystem jumpParticles;
    //[SerializeField] private ParticleSystem _launchParticles;
    //[SerializeField] private ParticleSystem moveParticles;
    //[SerializeField] private ParticleSystem landParticles;

    //[SerializeField] private PlayerController controller;
    //private bool grounded;
    //private bool onWall;
    //private ParticleSystem.MinMaxGradient _currentGradient;

    //private MovementStateMachine.MovementData movementData;

    //private void Awake()
    //{
    //    EventsHolder.PlayerEvents.OnPlayerJump += OnJumped;
    //    movementData = controller.MovementMachine.Data;

    //    if(movementData != null) movementData.OnChangeGround += OnGroundedChanged;
    //    moveParticles.Play();
    //}

    //private void OnDestroy()
    //{
    //    if (movementData != null) movementData.OnChangeGround -= OnGroundedChanged;

    //    moveParticles.Stop();
    //}


    //private float frameInputHorizontalMove;
    //private void Update()
    //{
    //    if (controller == null) return;

    //    HandleSpriteFlip();

    //    HandleIdleSpeed();

    //    HandleCharacterTilt();
    //}

    //private void HandleSpriteFlip()
    //{
    //    if (frameInputHorizontalMove != 0) sprite.flipX = frameInputHorizontalMove < 0;
    //}

    //private void HandleIdleSpeed()
    //{
    //    var inputStrength = Mathf.Abs(frameInputHorizontalMove);
    //    animator.SetFloat(IdleSpeedKey, Mathf.Lerp(1, maxIdleSpeed, inputStrength));
    //    moveParticles.transform.localScale = Vector3.MoveTowards(moveParticles.transform.localScale, Vector3.one * inputStrength, 2 * Time.deltaTime);
    //}

    //private void HandleCharacterTilt()
    //{
    //    var runningTilt = grounded
    //        ? Quaternion.Euler(0, 0, _maxTilt * frameInputHorizontalMove)
    //        : Quaternion.identity;
    //    animator.transform.up = Vector3.RotateTowards(animator.transform.up, runningTilt * Vector2.up, _tiltSpeed * Time.deltaTime, 0f);
    //}

    //private void OnJumped(TraversableTerrain _)
    //{
    //    animator.SetTrigger(JumpKey);
    //    animator.ResetTrigger(GroundedKey);

    //    if (grounded) // Avoid coyote
    //    {
    //        jumpParticles.Play();
    //    }
    //}

    //private void OnGrabWall(TraversableTerrain _)
    //{

    //}

    //private void OnGroundedChanged(bool newGrounded, float impact, TraversableTerrain _)
    //{
    //    grounded = newGrounded;

    //    if (newGrounded)
    //    {
    //        animator.SetTrigger(GroundedKey);
    //        //source.PlayOneShot(footsteps[Random.Range(0, footsteps.Length)]);
    //        moveParticles.Play();

    //        landParticles.transform.localScale = Vector3.one * Mathf.InverseLerp(0, 40, impact);
    //        landParticles.Play();
    //    }
    //    else
    //    {
    //        moveParticles.Stop();
    //    }
    //}

    //private void OnWallChanged(bool newIsOnWall, float grabPoint)
    //{
    //    onWall = newIsOnWall;

    //    if (newIsOnWall)
    //    {
    //        animator.SetTrigger(WallKey);
    //        //source.PlayOneShot(footsteps[Random.Range(0, footsteps.Length)]);
    //        //source.PlayOneShot(wallSlide);
    //        moveParticles.Play();

    //        landParticles.transform.localScale = Vector3.one;
    //        landParticles.Play();
    //    }
    //    else
    //    {
    //        moveParticles.Stop();
    //    }
    //}

    //private static readonly int GroundedKey = Animator.StringToHash("Grounded");
    //private static readonly int WallKey = Animator.StringToHash("OnWall");
    //private static readonly int IdleSpeedKey = Animator.StringToHash("IdleSpeed");
    //private static readonly int JumpKey = Animator.StringToHash("Jump");
}
