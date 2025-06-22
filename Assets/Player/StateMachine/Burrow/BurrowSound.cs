using System.Collections;
using UnityEngine;
using UnityEngine.Assertions.Must;
using static TransitionLibrary;

public class BurrowSound : IMovementObserverState<BurrowMovement>
{
    public BurrowMovement MovementState { get; set; }

    private readonly BurrowSoundStats stats;
    private readonly SoundFXManager sfxManager;
    private readonly AudioSource loopingSource;
    private readonly Transform transform;

    public BurrowSound(BurrowMovement burrowMovement, SoundInitData soundData)
    {
        transform = soundData.Transform;
        stats = soundData.Stats.burrowStats;
        sfxManager = soundData.SoundFXManager;
        if (sfxManager) { loopingSource = sfxManager.GetLoopingSFX(transform); }

        MovementState = burrowMovement;
        MovementState.OnPlayerBounce +=  (Vector2 vel) => 
        {
            if (sfxManager)
            {
                sfxManager.PlaySFX(stats.bounce, transform.position);
            }
        };
        MovementState.OnBurrowDash += () =>
        {
            OnBurrowDash();
            entryDashStoppedOrInterrupted = true;
            isDashing = true;
        };
    }

    private float time;
    private bool entryDashStoppedOrInterrupted;
    private bool isDashing;
    public void Update(MovementInput _)
    {
        justEntered = false;

        time += Time.deltaTime;
        if (loopingSource){
            loopingSource.volume = Mathf.Lerp(loopingSource.volume,
                (MovementState.IsBurrowDashing && time >= stats.timeToSandExitSound) ? stats.dash.volume : stats.loopingBurrow.volume,
                time / stats.burrowFadeInTime);
        }
        if (!MovementState.IsBurrowDashing) { entryDashStoppedOrInterrupted = true; isDashing = false; }
    }

    float lastBurrowTime;
    float pitchProgress;
    int entries;
    bool justEntered;

    private void OnBurrowDash()
    {
        if (!sfxManager || justEntered) return;

        SoundFX dash = (SoundFX)stats.dash.Clone();
        dash.volume *= isDashing ? 0.5f : 1;
        sfxManager.PlaySFX(dash, transform.position);
    }
    public void EnterState(IStateSpecificTransitionData data)
    {
        justEntered = true;
        entries++;
        if (sfxManager)
        {
            bool enteredDirectly = data is BurrowMovement.BurrowMovementTransitionData burrowData
                                   && burrowData.EnteredDirectly;

            pitchProgress = Mathf.Repeat(entries, stats.burrowNumToMaxPitch);

            SoundFX snowEntry = enteredDirectly ? (SoundFX)stats.directEntry.Clone() : (SoundFX)stats.entry.Clone();
            snowEntry.pitchRange = Vector2.one * Mathf.Lerp(snowEntry.pitchRange.x, snowEntry.pitchRange.y, pitchProgress / stats.burrowNumToMaxPitch);
            sfxManager.PlaySFX(snowEntry, transform.position);
        }

        if (loopingSource)
        {
            SoundFXManager.ChangeSourceSound(loopingSource, stats.loopingBurrow);
            loopingSource.volume = 0f;
            loopingSource.Play();
            loopingSource.time = lastBurrowTime;
        }
    }
    public void ExitState() 
    {
        if (loopingSource)
        {
            lastBurrowTime = loopingSource.time;
            loopingSource.Stop();
        }
        if (sfxManager)
        {
            if (entryDashStoppedOrInterrupted)
            {
                SoundFX snowExit = (SoundFX)stats.exit.Clone();
                float dashMult = MovementState.IsBurrowDashing ? 2 : 1;

                snowExit.pitchRange = Vector2.one * Mathf.Lerp(snowExit.pitchRange.x, snowExit.pitchRange.y, pitchProgress / stats.burrowNumToMaxPitch);
                snowExit.volume = dashMult * Mathf.Lerp(0, snowExit.volume, time / stats.timeToSandExitSound);
                sfxManager.PlaySFX(snowExit, transform.position);
            }
        }

        time = 0;
        entryDashStoppedOrInterrupted = false;
    }
}