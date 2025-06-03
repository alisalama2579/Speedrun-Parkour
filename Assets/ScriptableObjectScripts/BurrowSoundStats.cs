using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BurrowSoundStats", menuName = "BurrowSoundStats")]
public class BurrowSoundStats : ScriptableObject
{
    public float burrowFadeInTime;
    public float timeToSandExitSound;

    public SoundFX loopingBurrow;
    public float burrowNumToMaxPitch;

    public SoundFX dash;
    public SoundFX entry;
    public SoundFX directEntry;
    public SoundFX bounce;
    public SoundFX exit;
    public SoundFX dashExit;
}
