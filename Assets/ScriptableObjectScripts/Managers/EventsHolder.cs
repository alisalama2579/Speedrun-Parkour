using System;
using UnityEngine;

public class EventsHolder
{
    public static event Action OnPlayerLandOnStableGround;
    public static void InvokePlayerLandOnStableGround() => OnPlayerLandOnStableGround?.Invoke();

    public static event Action OnPlayerDeath;
    public static void InvokePlayerDeath() => OnPlayerDeath?.Invoke();

    public static event Action OnRaceStart;
    public static void InvokeRaceStart() => OnRaceStart?.Invoke();

    public static event Action OnRaceEnd;
    public static void InvokeRaceEnd() => OnRaceEnd?.Invoke();
}
