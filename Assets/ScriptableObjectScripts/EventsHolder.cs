using System;
public class EventsHolder
{
    public static class GameEvents
    {
    }

    public static class PlayerEvents
    {
        public static Action<TraversableTerrain> OnPlayerLandOnGround;

        public static Action<TraversableTerrain> OnPlayerGrabWall;

        public static Action<TraversableTerrain> OnPlayerJump;
        public static Action<TraversableTerrain> OnPlayerWallJump;

        public static Action<ISand> OnPlayerEnterSand;
        public static Action<ISand> OnPlayerExitSand;

        public static Action OnPlayerDeath;
    }
}
