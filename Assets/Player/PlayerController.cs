using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Player))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;


    private PlayerStateMachine stateMachine;
    public PlayerStateMachine StateMachine => stateMachine;

    private Player player;
    private Animator anim;
    [SerializeField] private MovementStatsHolder movementStats;
    [SerializeField] private AnimationStatsHolder animationStats;
    [SerializeField] private PlayerSoundStats soundStats;
    [SerializeField] private SoundFXManager sfxManager;


    public Player.Input FrameInput => player.FrameInput;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();

        stateMachine = new PlayerStateMachine(typeof(LandMovement), transform, rb, col, movementStats, animationStats, anim, soundStats, sfxManager);
    }


    private void OnDisable()
    {
        stateMachine = null;
    }

    private void Update()
    {
        stateMachine?.Update(FrameInput);
    }

    private void FixedUpdate()
    {
        stateMachine?.FixedUpdate();
    }

    private void OnDestroy()
    {
        stateMachine?.OnDestroy();
    }

    #region Collisions

    //Collisions
    private void OnCollisionEnter2D(Collision2D collision) => ProcessEntryCollisions(collision: collision);
    private void OnTriggerEnter2D(Collider2D trigger) => ProcessEntryCollisions(trigger: trigger);
    private void OnCollisionExit2D(Collision2D collision) => ProcessExitCollisions(collision: collision);
    private void OnTriggerExit2D(Collider2D trigger) => ProcessExitCollisions(trigger: trigger);

    private void ProcessEntryCollisions(Collider2D trigger = null, Collision2D collision = null)
    {
        //Triggers
        if (trigger != null)
        {
            if (trigger.TryGetComponent(out IPlayerCollisionListener collisionListener)) 
            {
                if (collisionListener is DamageDealer) player.OnDeath();

                collisionListener.OnPlayerEnter();
                stateMachine?.TriggerEnter(collisionListener);
            }
            stateMachine?.TriggerEnter(trigger);
        }

        //Colliders
        else if (collision != null)
        {
            if (collision.transform.TryGetComponent(out IPlayerCollisionListener collisionListener)) 
            {
                if (collisionListener is DamageDealer) player.OnDeath();

                collisionListener.OnPlayerEnter();
                stateMachine?.CollisionEnter(collisionListener);
            }
            stateMachine?.CollisionEnter(collision);
        }
    }
    private void ProcessExitCollisions(Collider2D trigger = null, Collision2D collision = null)
    {
        //Triggers
        if (trigger != null)
        {
            if (trigger.TryGetComponent(out IPlayerCollisionListener collisionListener))
            { 
                collisionListener.OnPlayerExit();
                stateMachine?.TriggerExit(collisionListener);
            }
            stateMachine?.TriggerExit(trigger);
        }

        //Colliders
        else if (collision != null)
        {
            if (collision.transform.TryGetComponent(out IPlayerCollisionListener collisionListener)) 
            { 
                collisionListener.OnPlayerExit();
                stateMachine?.CollisionExit(collisionListener);
            }
            stateMachine?.CollisionExit(collision);
        }
    }

    #endregion
}
