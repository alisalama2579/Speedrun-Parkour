using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Player))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D col;
    private MovementStateMachine movementMachine;
    public MovementStateMachine MovementMachine => movementMachine;

    [SerializeField] private MovementStatsHolder movementStats;
    [SerializeField] private Player player;
    public Player.Input FrameInput => player.FrameInput;

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();

        movementMachine = new MovementStateMachine(typeof(LandMovement), movementStats, rb, col);
    }


    private void OnDisable()
    {
        movementMachine = null;
    }

    private void Update()
    {
        movementMachine?.Update(FrameInput);
    }

    private void FixedUpdate()
    {
        movementMachine?.FixedUpdate();
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
                movementMachine?.TriggerEnter(collisionListener);
            }
            movementMachine?.TriggerEnter(trigger);
        }

        //Colliders
        else if (collision != null)
        {
            if (collision.transform.TryGetComponent(out IPlayerCollisionListener collisionListener)) 
            {
                if (collisionListener is DamageDealer) player.OnDeath();

                collisionListener.OnPlayerEnter();
                movementMachine?.CollisionEnter(collisionListener);
            }
            movementMachine?.CollisionEnter(collision);
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
                movementMachine?.TriggerExit(collisionListener);
            }
            movementMachine?.TriggerExit(trigger);
        }

        //Colliders
        else if (collision != null)
        {
            if (collision.transform.TryGetComponent(out IPlayerCollisionListener collisionListener)) 
            { 
                collisionListener.OnPlayerExit();
                movementMachine?.CollisionExit(collisionListener);
            }
            movementMachine?.CollisionExit(collision);
        }
    }

    #endregion
}
