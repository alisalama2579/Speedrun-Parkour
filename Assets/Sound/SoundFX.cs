using System;
using UnityEngine;

[Serializable]
public class SoundFX : ICloneable
{
    public AudioClip clip;
    public float volume = 1;
    public Vector2 pitchRange = Vector2.one;

    public object Clone() => this.MemberwiseClone();

    public SoundFX(AudioClip clip, Vector2 pitchRange, float volume = 1)
    {
        this.clip = clip;
        this.volume = volume;
        this.pitchRange = pitchRange;
    }
}