using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerSoundStats", menuName = "PlayerSoundStats")]
public class PlayerSoundStats : ScriptableObject
{
    [SerializeField] private SerializableDictionary<SoundType, SoundInfo> tempSounds;
    private Dictionary<SoundType, SoundInfo> sounds;   

    [SerializeField] public AudioClip loopingBurrow;
    [SerializeField] public AudioClip loopingWallSlide;

    [SerializeField] public SoundInfo[] footsteps;


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

    public SoundInfo GetSoundFromType(SoundType type) => sounds.GetValueOrDefault(type);

    public enum SoundType
    {
        WallGrab,
        WallJump,
        Jump,
        BurrowEnter,
        BurrowExit,
        Dash,
        Landed,
        SandEnter,
        SandExit
    }
}
