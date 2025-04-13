using System;
using UnityEngine;

[Serializable]
public class SoundInfo
{
    public AudioClip clip;
    public float volume = 1;
    public Vector2 pitchRange = Vector2.one;
}