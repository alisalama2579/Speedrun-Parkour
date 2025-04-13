using UnityEngine;
public static class Utility<T>
{
    public enum CardinalDirections
    {
        Up,
        Down, 
        Left, 
        Right
    }


    public static Vector2 Vector2Slerp(Vector2 start, Vector2 end, float maxDelta)
    {
        // Dot product - the cosine of the angle between 2 vectors.
        float dot = Vector2.Dot(start, end);

        // Clamp it to be in the range of Acos()
        dot = Mathf.Clamp(dot, -1.0f, 1.0f);

        // Acos(dot) returns the angle between start and end,
        // And multiplying that by percent returns the angle between
        // start and the final result.
        float theta = Mathf.Acos(dot) * maxDelta;
        Vector2 relativeVec = end - start * dot;
        relativeVec.Normalize();

        // The final result.
        return ((start * Mathf.Cos(theta)) + (relativeVec * Mathf.Sin(theta)));
    }

    public static T GetRandomFromArray (params T[] array) => array[Random.Range(0, array.Length)];
}
