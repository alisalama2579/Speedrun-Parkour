using System;
using UnityEngine;

[Serializable]
public class SoundFX : ICloneable
{
    public AudioClip clip;
    public float volume = 1;

    public object Clone() => this.MemberwiseClone();
}

public class PitchedSoundFX : SoundFX
{
    public Vector2 pitchRange = Vector2.one;
}