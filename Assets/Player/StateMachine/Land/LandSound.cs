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
        MovementState.OnChangeGround += (bool a, float b, TraversableTerrain c) =>{
            OnChangeGrounded(a, b, c);
        };
        MovementState.OnLeap += () =>{
            OnLeap();
        };
        MovementState.OnPlayerChangedWall += (bool a, TraversableTerrain c) => {
            OnChangeWall(a, c);
        };
        MovementState.OnWallJump += (TraversableTerrain a) => {
            OnWallJump(a);
        };
        MovementState.OnJump += (TraversableTerrain a) => {
            OnJump(a);
        };
    }

    public void ExitState()
    {
        footstepProgress = 0;
        time = 0;
        if(loopingSource) loopingSource.Stop();
    }


    float deltaTime;
    float time;
    public void Update(MovementInput _)
    {
        if (sfxManager == null) return;
        time += Time.deltaTime;
        loopingSource.volume = Mathf.Lerp(loopingSource.volume, stats.loopingWallSlide.volume,
            time / stats.wallSlideFadeInTime);
    }
    public void FixedUpdate()
    {
        if (sfxManager == null) return;

        deltaTime = Time.deltaTime;
        HandleFootSteps();
    }

    private float footstepProgress;
    private void HandleFootSteps()
    {
        if (MovementState.IsGrounded)
        {
            footstepProgress += Mathf.Abs(MovementState.HorizontalVel * deltaTime);
            if (footstepProgress < stats.footstepInterval)
                return;

            var sound = Utility.GetRandomFromArray<SoundFX>(stats.footsteps);
            if (sfxManager) { sfxManager.PlaySFX(sound, MovementState.Pos); }
            Debug.Log("took step ");
            footstepProgress = 0;
        }
        else
            footstepProgress = 0;
    }
    private void OnJump(TraversableTerrain _) { if (sfxManager) { sfxManager.PlaySFX(stats.jump, MovementState.Pos); } }
    private void OnWallJump(TraversableTerrain _) { if (sfxManager) { sfxManager.PlaySFX(stats.wallJump, MovementState.Pos); }}
    private void OnLeap(){ if (sfxManager) { sfxManager.PlaySFX(Utility.GetRandomFromArray(stats.leaps), MovementState.Pos); } }
    private void OnChangeGrounded(bool newGrounded, float impact, TraversableTerrain _)
    {
        if (newGrounded)
        {
            SoundFX sound = (SoundFX)stats.land.Clone();
            sound.volume *= impact;

            if (sfxManager) { sfxManager.PlaySFX(sound, MovementState.Pos); }
        }
    }
    private void OnChangeWall(bool newWall, TraversableTerrain terrain)
    {
        if (!loopingSource || !sfxManager) return;

        if (newWall)
        {
            sfxManager.PlaySFX(GetSurfaceSpecificSound(stats.wallGrabs, terrain), MovementState.Pos);

            SoundFX sound = (SoundFX)GetSurfaceSpecificSound(stats.wallSlides, terrain).Clone();
            SoundFXManager.ChangeSourceSound(loopingSource, sound);
            loopingSource.volume = 0;
            loopingSource.Play();
        }
        else 
            loopingSource.Stop();
    }


    private SoundFX GetSurfaceSpecificSound(SoundFX[] sounds, TraversableTerrain terrain)
    {
        if (sounds == null || terrain == null) return null;

        int index = Mathf.Clamp((int)terrain.soundType, 0, sounds.Length - 1);
        return sounds[index];
    }
}