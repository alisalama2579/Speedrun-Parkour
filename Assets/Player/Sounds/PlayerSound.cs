using System;
using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    [SerializeField] private PlayerController controller;
    [SerializeField] private PlayerSoundStats stats;
    [SerializeField] private SoundFXManager sfx;

    public float footstepTimeInterval;
    private AudioSource loopingSource;


    private void Awake()
    {
        if (sfx == null)
            Destroy(this);

        HandleSoundEventSubscription(true);
        loopingSource = sfx.GetLoopingSFX(transform);
    }
    private void OnDestroy() => HandleSoundEventSubscription(false);
    private void HandleSoundEventSubscription(bool subscribe)
    {
        var movementData = controller.MovementMachine.Data;

        movementData.OnChangeWall = subscribe ?
            movementData.OnChangeWall + OnChangeWall
            : movementData.OnChangeWall - OnChangeWall;

        movementData.OnJump = subscribe ?
            movementData.OnJump + OnJump
            : movementData.OnJump - OnJump;

        movementData.OnChangeGround = subscribe ?
            movementData.OnChangeGround + OnChangeGrounded
            : movementData.OnChangeGround - OnChangeGrounded;

        movementData.OnWallJump = subscribe ?
             movementData.OnWallJump + OnWallJump :
             movementData.OnWallJump - OnWallJump;

        movementData.OnDash = subscribe ?
           movementData.OnDash + OnDash :
           movementData.OnDash - OnDash;

        movementData.OnMove = subscribe ?
           movementData.OnMove + OnMove :
           movementData.OnMove - OnMove;

        movementData.OnBurrowMovementEnter = subscribe ?
           movementData.OnBurrowMovementEnter + OnBurrowMovementEnter :
           movementData.OnBurrowMovementEnter - OnBurrowMovementEnter;

        movementData.OnBurrowMovementExit = subscribe ?
           movementData.OnBurrowMovementExit + OnBurrowMovementExit :
           movementData.OnBurrowMovementExit - OnBurrowMovementExit;

        EventsHolder.PlayerEvents.OnPlayerEnterSand = subscribe ?
             EventsHolder.PlayerEvents.OnPlayerEnterSand + OnSandEnter :
              EventsHolder.PlayerEvents.OnPlayerEnterSand - OnSandEnter;

        EventsHolder.PlayerEvents.OnPlayerExitSand = subscribe ?
          EventsHolder.PlayerEvents.OnPlayerExitSand + OnSandExit :
           EventsHolder.PlayerEvents.OnPlayerExitSand - OnSandExit;
    }


    #region SoundPlaying
    private void OnMove(float speed, TraversableTerrain terrainOn) => sfx.PlaySFX(Utility<SoundInfo>.GetRandomFromArray(stats.footsteps), transform.position);

    private void OnChangeWall(bool newIsOnWall, TraversableTerrain terrain)
    {
        if (newIsOnWall)
        {
            sfx.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.WallGrab), transform.position);
            loopingSource.clip = stats.loopingWallSlide;
            loopingSource.Play();
        }
        else 
            loopingSource.Stop();
    }

    private void OnChangeGrounded(bool newIsGrounded, float impact, TraversableTerrain terrain)
    {
        if (newIsGrounded)
            sfx.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.Landed), transform.position);
    }

    private void OnBurrowMovementEnter()
    {
        sfx.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.BurrowEnter), transform.position);

        loopingSource.clip = stats.loopingBurrow;
        loopingSource.Play();
    }
    private void OnBurrowMovementExit()
    {
        sfx.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.BurrowExit), transform.position);
        loopingSource.Stop();
    }

    private void OnSandEnter(ISand sand) => sfx.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.SandEnter), transform.position);
    private void OnSandExit(ISand sand) => sfx.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.SandExit), transform.position);

    private void OnDash() => sfx.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.Dash), transform.position);
    private void OnJump() => sfx.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.Jump), transform.position);
    private void OnWallJump() => sfx.PlaySFX(stats.GetSoundFromType(PlayerSoundStats.SoundType.WallJump), transform.position);

    #endregion
}
