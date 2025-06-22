using System;
using System.Diagnostics;
using System.Persistence;
using UnityEngine;
  
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Player))]
public class PlayerController : MonoBehaviour, IInteractionProvider
{
    private Rigidbody2D rb;
    private Collider2D col;

    private PlayerStateMachine stateMachine;
    public PlayerStateMachine StateMachine => stateMachine;

    private Player player;
    [SerializeField] private PlayerProperties properties;
    [SerializeField] private Animator anim;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private MovementStatsHolder movementStats;
    [SerializeField] private AnimationStatsHolder animationStats;
    [SerializeField] private PlayerSoundStats soundStats;
    [SerializeField] private SoundFXManager sfxManager;

    public MovementInput movementInput = new();

    private void Awake()
    {
        col = GetComponent<Collider2D>();
        rb = GetComponent<Rigidbody2D>();
        player = GetComponent<Player>();

        VisualsInitData visData = new ()
        {
            Anim = anim,
            Transform = anim.transform,
            Stats = animationStats,
            Renderer = spriteRenderer
        };

        SoundInitData soundData = new()
        {
            SoundFXManager = sfxManager,
            Transform = transform,
            Stats = soundStats,
        };

        MovementInitData movementData = new()
        {
            Transform = transform,
            RB = rb,
            Col = col,
            Stats = movementStats,
            Properties = properties.movementProperties
        };

        stateMachine = new PlayerStateMachine(typeof(LandMovement), movementData, visData, soundData);
        stateMachine.AddAnyTransition(typeof(RaceReadyMovement), TransitionToRaceState);

        IRaceController.OnRaceEnter += (IRaceController race) =>{
            raceEnterTriggered = true;
        };
    }

    private bool raceEnterTriggered;
    public TransitionLibrary.IStateSpecificTransitionData TransitionToRaceState ()
    {
        if(raceEnterTriggered)
        {
            properties.enteredRaceStartPosition.Value = false;
            raceEnterTriggered = false;
            return new TransitionLibrary.SuccesfulTransitionData();
        }

        return new TransitionLibrary.FailedTransitionData();
    }

    private void OnDestroy()
    {
        stateMachine?.OnDestroy();
        stateMachine = null;
    }

    private void Update()
    {
        movementInput.Update(player.controls);
        stateMachine?.Update(movementInput);
    }

    private void FixedUpdate()
    {
        stateMachine?.FixedUpdate();
    }

    #region Collisions

    //Collisions

    public event Action<IPlayerCollisionInteractor> OnCollisionInteraction;

    private void OnCollisionEnter2D(Collision2D collision) => ProcessEntryCollisions(collision: collision);
    private void OnTriggerEnter2D(Collider2D trigger) => ProcessEntryCollisions(trigger: trigger);
    private void OnCollisionExit2D(Collision2D collision) => ProcessExitCollisions(collision: collision);
    private void OnTriggerExit2D(Collider2D trigger) => ProcessExitCollisions(trigger: trigger);
    private void OnCollisionStay2D(Collision2D collision) => ProcessStayCollisions(collision: collision);
    private void OnTriggerStay2D(Collider2D trigger) => ProcessStayCollisions(trigger: trigger);

    private void ProcessEntryCollisions(Collider2D trigger = null, Collision2D collision = null)
    {
        IPlayerCollisionListener listener;
        IPlayerCollisionInteractor interactor = null;

        //Triggers
        if (trigger != null)
        {
            if (trigger.TryGetComponent(out listener))
            {
                listener.OnPlayerEnter();
            }

            if (trigger.transform.TryGetComponent(out interactor))
            {
                if (interactor is RaceStart && properties.enteredRaceStartPosition != null)
                    properties.enteredRaceStartPosition.Value = true;

                stateMachine?.TriggerEnter(interactor);
                stateMachine?.TriggerEnter(trigger);
            }
        }

        //Colliders
        else if (collision != null)
        {
            if (collision.transform.TryGetComponent(out listener))
                listener.OnPlayerEnter();

            if(collision.transform.TryGetComponent(out interactor))
            {
                stateMachine?.CollisionEnter(interactor);
                stateMachine?.CollisionEnter(collision);
            }
        }

        ProcessInteractions(interactor);
    }
    private void ProcessExitCollisions(Collider2D trigger = null, Collision2D collision = null)
    {
        IPlayerCollisionListener listener;
        IPlayerCollisionInteractor interactor = null;

        //Triggers
        if (trigger != null)
        {
            if (trigger.TryGetComponent(out listener))
            {
                listener.OnPlayerExit();
            }

            if (trigger.TryGetComponent(out interactor))
            {
                if (interactor is RaceStart && properties.enteredRaceStartPosition != null)
                    properties.enteredRaceStartPosition.Value = false;

                stateMachine?.TriggerExit(interactor);
                stateMachine?.TriggerExit(trigger);
            }
        }

        //Colliders
        else if (collision != null)
        {
            if (collision.transform.TryGetComponent(out listener))
            {
                listener.OnPlayerExit();
            }

            if (collision.transform.TryGetComponent(out interactor))
            {
                stateMachine?.CollisionExit(interactor);
                stateMachine?.CollisionExit(collision);
            }
        }

        ProcessInteractions(interactor);
    }
    private void ProcessStayCollisions(Collider2D trigger = null, Collision2D collision = null)
    {
        IPlayerCollisionListener listener;
        IPlayerCollisionInteractor interactor = null;

        //Triggers
        if (trigger != null)
        {
            if (trigger.TryGetComponent(out listener))
            {
                listener.OnPlayerStay();
            }

            trigger.transform.TryGetComponent(out interactor);
        }

        //Colliders
        else if (collision != null)
        {
            if (collision.transform.TryGetComponent(out listener))
                listener.OnPlayerStay();

            collision.transform.TryGetComponent(out interactor);
        }

        ProcessInteractions(interactor);
    }

    private void ProcessInteractions(IPlayerCollisionInteractor interactor) 
    {
        if (interactor == null) return;
        OnCollisionInteraction?.Invoke(interactor);
    }

    #endregion
}
