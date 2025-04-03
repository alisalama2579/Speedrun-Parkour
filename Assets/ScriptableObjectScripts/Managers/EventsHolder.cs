using System;
using UnityEngine;

public class EventsHolder
{
    public static class GameEvents
    {
        public static event Action OnRaceStart;
        public static void InvokeRaceStart() => OnRaceStart?.Invoke();

        public static event Action OnRaceEnd;
        public static void InvokeRaceEnd() => OnRaceEnd?.Invoke();
    }

    public static class PlayerEvents
    {
        public static event Action<TraversableTerrain> OnPlayerLandOnGround;
        public static void InvokePlayerLandOnGround(TraversableTerrain terrain) => OnPlayerLandOnGround?.Invoke(terrain);


        public static event Action<TraversableTerrain> OnPlayerGrabWall;
        public static void InvokePlayerGrabWall(TraversableTerrain terrain) => OnPlayerGrabWall?.Invoke(terrain);


        public static event Action<TraversableTerrain> OnPlayerCollideWithTerrain;
        public static void InvokePlayerCollideWithTerrain(TraversableTerrain terrain) => OnPlayerCollideWithTerrain?.Invoke(terrain);


        public static event Action<ISand> OnPlayerBurrow;
        public static void InvokePlayerBurrow(ISand sand) => OnPlayerBurrow.Invoke(sand);


        public static event Action OnPlayerDeath;
        public static void InvokePlayerDeath() => OnPlayerDeath?.Invoke();
    }
}
