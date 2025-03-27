using UnityEngine;

public class BurrowSand : TraversableTerrain, ISand, IWallGrabbable
{
    public bool IsBurrowable => true;
    public void OnSandTargetForBurrow(Vector2 _) { }
    public void OnSandBurrowExit(Vector2 _) {  }
    public void OnSandBurrowEnter(Vector2 _) {  }
}
