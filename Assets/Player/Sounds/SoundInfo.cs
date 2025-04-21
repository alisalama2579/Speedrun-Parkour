using System;
using UnityEngine;

[Serializable]
public class SoundInfo : ICloneable
{
    public AudioClip clip;
    public float volume = 1;
    public Vector2 pitchRange = Vector2.one;

    public object Clone() => this.MemberwiseClone();
}