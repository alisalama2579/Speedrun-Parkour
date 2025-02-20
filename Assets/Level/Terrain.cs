using UnityEngine;

public class Terrain : MonoBehaviour
{
    public bool isSlippery;
    public bool isWall;

    private Color color;
    private void Awake() => color = GetComponent<SpriteRenderer>().color;
}
