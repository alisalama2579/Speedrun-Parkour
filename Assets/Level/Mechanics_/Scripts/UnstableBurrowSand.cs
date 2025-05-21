using System;
using UnityEngine;

public class UnstableBurrowSand : UnstablePlatform, ISand
{
    public bool IsBurrowable => !fadeTriggered && col.enabled;
    public float LaunchSpeed => stats.sandLaunchSpeed;
    public float WeakLaunchSpeed => LaunchSpeed;

    public void OnSandEnter(Vector2 _, Vector2 pos) { }
    public void OnSandExit(Vector2 _, Vector2 pos) { }
    public void OnSandTargetForBurrow(Vector2 vel){ StartFade(stats.fastFadeTime, true);}
}