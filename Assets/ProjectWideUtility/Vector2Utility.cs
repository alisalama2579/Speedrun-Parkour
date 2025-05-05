using UnityEngine;
public static class Vector2Utility
{
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

    public static float GetVector2Angle(Vector2 v) => Mathf.Rad2Deg * Mathf.Atan2(v.x, v.y);
    public static float GetUnityVector2Angle(Vector2 v) => Mathf.Rad2Deg * Mathf.Atan2(v.y, v.x);
    public static float GetVector2Radian(Vector2 v) => Mathf.Atan2(v.x, v.y);

    public static Vector2 GetAngleVector2(float theta) => new (Mathf.Cos(theta), Mathf.Sin(theta));
    public static Vector2 GetRadianVector2(float theta) => new (Mathf.Cos(theta * Mathf.Rad2Deg), Mathf.Sin(theta * Mathf.Rad2Deg));
}
