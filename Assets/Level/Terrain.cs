using UnityEngine;

public class Terrain : MonoBehaviour
{
    public bool isSlippery;
    public bool isWall;
    public bool isMoving;

    private void OnValidate()
    {
        isSlippery = (!isWall && !isMoving) || isSlippery;
    }

    public Color color;
    private void Awake() => color = GetComponent<SpriteRenderer>().color;
}
