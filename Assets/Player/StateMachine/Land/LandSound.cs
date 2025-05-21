using UnityEngine;
using System;

public class LandSound : IMovementObserverState<LandMovement>
{
    private readonly PlayerSoundStats stats;
    private readonly SoundFXManager sfxManager;
    private readonly AudioSource loopingSource;
    private readonly Transform transform;

    public LandMovement MovementState { get; set; }

    public LandSound(LandMovement landMovement, SoundInitData soundData)
    {
        transform = soundData.Transform;
        stats = soundData.Stats;
        sfxManager = soundData.SoundFXManager;
        if (sfxManager) { loopingSource = sfxManager.GetLoopingSFX(transform); }

        MovementState = landMovement;

    }
    float deltaTime;
    public void Update(MovementInput _)
    {
        if (sfxManager == null) return;

        deltaTime = Time.deltaTime;
        HandleFootSteps();
    }

    private float footstepProgress;
    public void HandleFootSteps()
    {
        if (MovementState.IsGrounded)
        {
            footstepProgress += deltaTime;
            if (footstepProgress < stats.footstepInterval)
                return;

            var sound = Utility.GetRandomFromArray<SoundFX>(stats.footsteps);
            if (sfxManager) { sfxManager.PlaySFX(sound, MovementState.Pos); }
            footstepProgress = 0;
        }
        else
            footstepProgress = 0;
    }

    public void OnDash() { if (sfxManager) { sfxManager.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.Dash), MovementState.Pos); } }
    public void OnEntryLaunch(Vector2 vel) { if (sfxManager) { sfxManager.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.EntryLaunch), MovementState.Pos); } }
    public void OnJump() { if (sfxManager) { sfxManager.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.Jump), MovementState.Pos); } }
    public void OnWallJump() { if (sfxManager) { sfxManager.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.WallJump), MovementState.Pos); }}
    
    public void OnChangeGrounded(bool newGrounded, float impact, TraversableTerrain _)
    {
        if (newGrounded)
        {
            SoundFX sound = (SoundFX)stats.GetSoundFromType(PlayerSoundStats.SoundType.Landed).Clone();
            sound.volume *= impact;

            if (sfxManager) { sfxManager.PlaySFX(sound, MovementState.Pos); }
        }
    }
    public void OnChangeWall(bool newWall, TraversableTerrain _)
    {
        if (!loopingSource) return;

        if (newWall)
        {
            SoundFX sound = (SoundFX)stats.GetSoundFromType(PlayerSoundStats.SoundType.WallGrab).Clone();
            if (sfxManager) { sfxManager.PlaySFX(sound, MovementState.Pos); }

            loopingSource.clip = stats.loopingWallSlide;
            loopingSource.Play();
        }
        else 
            loopingSource.Stop();
    }
}