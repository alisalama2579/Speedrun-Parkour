using UnityEngine;
using System;

public class SandEntrySound : IMovementObserverState<SandEntryMovement>
{
    private readonly PlayerSoundStats stats;
    private readonly SoundFXManager sfxManager;
    private readonly AudioSource loopingSource;
    private readonly Transform transform;

    public SandEntryMovement MovementState { get; set; }

    public SandEntrySound(SandEntryMovement movement, SoundInitData soundData)
    {
        transform = soundData.Transform;
        stats = soundData.Stats;
        sfxManager = soundData.SoundFXManager;
        if (sfxManager) { loopingSource = sfxManager.GetLoopingSFX(transform); }

        MovementState = movement;
    }

    private int entries;
    public void EnterState(TransitionLibrary.IStateSpecificTransitionData _)
    {
        //entries++;
        //if (sfxManager)
        //{
        //    float progress = Mathf.Repeat(entries, stats.burrowPitchIncreaseNum);
        //    Debug.Log(progress);
        //    SoundFX sandEntry = (SoundFX)stats.sandEntry.Clone();
        //    sandEntry.pitchRange = Vector2.one * Mathf.Lerp(stats.sandEntry.pitchRange.x, stats.sandEntry.pitchRange.y, progress / stats.burrowPitchIncreaseNum);
        //    Debug.Log(sandEntry.pitchRange);
        //    sfxManager.PlaySFX(sandEntry, transform.position);
        //}
    }
    public void ExitState()
    {

    }
}