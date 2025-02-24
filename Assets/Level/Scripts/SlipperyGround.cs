using UnityEngine;

public class SlipperyGround : TraversableTerrain
{
    [HideInInspector] public bool isSlippery = true;
    public void Melt() => isSlippery = false;

}
