using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSoundStats", menuName = "PlayerSoundStats")]
public class PlayerSoundStats : ScriptableObject
{
    [SerializeField] private SerializableDictionary<SoundType, SoundFX> tempSounds;
    private Dictionary<SoundType, SoundFX> sounds;   

    public AudioClip loopingBurrow;
    public AudioClip loopingWallSlide;

    public SoundFX[] footsteps;
    public float footstepInterval;


    private bool initialized;
    private void OnValidate() => TryInitialize();
    private void Awake() => TryInitialize();

    private void TryInitialize()
    {
        if (!initialized) return;

        initialized = true;
        sounds = tempSounds.GenerateDictionary();
        tempSounds = null;
    }

    public SoundFX GetSoundFromType(SoundType type) => sounds.GetValueOrDefault(type);

    public enum SoundType
    {
        WallGrab,
        WallJump,
        Jump,
        EntryLaunch,
        BurrowEnter,
        Dash,
        Landed,
    }
}
