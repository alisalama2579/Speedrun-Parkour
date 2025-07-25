using UnityEngine;

public static class Utility
{
    public enum CardinalDirections
    {
        Up,
        Down,
        Left,
        Right
    }

    public static T GetRandomFromArray<T>(params T[] array) => array[Random.Range(0, array.Length)];
    public static void DrawBox(Vector2 origin, Vector2 size, Vector2 dir, Color color)
    {
        const float HALF = 0.5f;

        Vector2 bottomLeft;
        Vector2 bottomRight;
        Vector2 topLeft;
        Vector2 topRight;

        if (dir == Vector2.zero)
        {
            topRight = origin + HALF * size;
            bottomLeft = origin + -HALF * size;

            topLeft = new Vector2(bottomLeft.x, topRight.y);
            bottomRight = new Vector2(topRight.x, bottomLeft.y);
        }
        else
        {
            Vector2 a = size.x * HALF * Vector2.Perpendicular(dir);
            Vector2 b = size.y * HALF * dir;

            bottomLeft = origin + a - b;
            bottomRight = origin - a - b;
            topLeft = origin + a + b;
            topRight = origin - a + b;
        }

        Debug.DrawLine(bottomLeft, bottomRight, color);
        Debug.DrawLine(bottomRight, topRight, color);
        Debug.DrawLine(topRight, topLeft, color);
        Debug.DrawLine(topLeft, bottomLeft, color);
    }
}
