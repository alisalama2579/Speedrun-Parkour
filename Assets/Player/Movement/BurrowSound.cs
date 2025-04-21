using System;
using UnityEngine;
using static TransitionLibrary;

public class BurrowSound : IMovementObserverState<BurrowMovement>
{
    public BurrowMovement MovementState { get; set; }

    private readonly PlayerSoundStats stats;
    private readonly SoundFXManager sfxManager;
    private readonly AudioSource loopingSource;
    private readonly Transform transform;

    public BurrowSound(BurrowMovement burrowMovement, Transform transform, PlayerSoundStats stats, SoundFXManager sfx)
    {
        this.transform = transform;
        this.stats = stats;

        sfxManager = sfx;
        loopingSource = sfx.GetLoopingSFX(transform);

        MovementState = burrowMovement;
    }

    public void EnterState()
    {
        loopingSource.clip = stats.loopingBurrow;
        loopingSource.Play();
    }
    public void ExitState() 
    {
        loopingSource.Stop();
    }
}