using UnityEngine;
using System;
using static TransitionLibrary;

public class LandSound : IMovementObserverState<LandMovement>
{
    private readonly PlayerSoundStats stats;
    private readonly SoundFXManager sfxManager;
    private readonly AudioSource loopingSource;
    private readonly Transform transform;

    public LandMovement MovementState { get; set; }

    public LandSound(LandMovement landMovement, Transform transform, PlayerSoundStats stats, SoundFXManager sfx)
    {
        this.stats = stats;
        sfxManager = sfx;
        loopingSource = sfx.GetLoopingSFX(transform);

        MovementState = landMovement;

    }
    float deltaTime;
    public void Update(MovementInput _) 
    {
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
            sfxManager.PlaySFX(sound, MovementState.Pos);
            footstepProgress = 0;
        }
        else
            footstepProgress = 0;
    }

    public void OnDash() => sfxManager.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.Dash), MovementState.Pos);
    public void OnEntryLaunch(Vector2 vel) => sfxManager.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.EntryLaunch), MovementState.Pos);
    public void OnJump(TraversableTerrain terrain) => sfxManager.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.Jump), MovementState.Pos);
    public void OnWallJump(TraversableTerrain terrain) => sfxManager.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.WallJump), MovementState.Pos);
    
    public void OnChangeGrounded(bool newGrounded, float impact, TraversableTerrain _)
    {
        if (newGrounded)
        {
            SoundFX sound = (SoundFX)stats.GetSoundFromType(PlayerSoundStats.SoundType.Landed).Clone();
            sound.volume *= impact;

            sfxManager.PlaySFX(sound, MovementState.Pos);
        }
    }
    public void OnChangeWall(bool newWall, TraversableTerrain _)
    {
        if (newWall)
        {
            SoundFX sound = (SoundFX)stats.GetSoundFromType(PlayerSoundStats.SoundType.WallGrab).Clone();
            sfxManager.PlaySFX(sound, MovementState.Pos);

            loopingSource.clip = stats.loopingWallSlide;
            loopingSource.Play();
        }
        else 
            loopingSource.Stop();
    }

}