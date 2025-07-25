using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSoundStats", menuName = "ScriptableObjects/Player/PlayerSoundStats")]
public class PlayerSoundStats : ScriptableObject
{
    public BurrowSoundStats burrowStats;

    public SoundFX loopingWallSlide;
    public float wallSlideFadeInTime;

    public SoundFX[] footsteps;
    public float footstepInterval;
    public float maxFootstepInterval;


    public SoundFX land;
    public SoundFX wallGrab;
    public SoundFX jump;
    public SoundFX wallJump;
    public SoundFX roll;
    public SoundFX[] leaps;

    [Header("Surface-Specific Sounds, consult Traversable Terrain order")]
    public SoundFX[] wallSlides;
    public SoundFX[] wallGrabs;
    public SoundFX[] wallJumps;
    public SoundFX[] lands;
}
