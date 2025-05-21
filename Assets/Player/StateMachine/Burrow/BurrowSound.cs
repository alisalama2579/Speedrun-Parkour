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

    public BurrowSound(BurrowMovement burrowMovement, SoundInitData soundData)
    {
        transform = soundData.Transform;
        stats = soundData.Stats;
        sfxManager = soundData.SoundFXManager;
        if (sfxManager) { loopingSource = sfxManager.GetLoopingSFX(transform); }

        MovementState = burrowMovement;
    }

    public void EnterState(IStateSpecificTransitionData _)
    {
        if (loopingSource)
        {
            loopingSource.clip = stats.loopingBurrow;
            loopingSource.Play();
        }
    }
    public void ExitState() 
    {
        if (loopingSource)
        {
            loopingSource.Stop();
        }
    }
}