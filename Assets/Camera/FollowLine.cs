using UnityEngine;

public class FollowLine : MonoBehaviour
{
    private FollowPoint[] points;
    private bool pointArrayValid = true;
    public float pointDotRange;

    private void Start()
    {
        points = GetComponentsInChildren<FollowPoint>();

        if (points == null || points.Length < 2) pointArrayValid = false;
        else InitializePoints();
    }

    private Vector2 GetClosestPointOnOneLine(FollowPoint A, FollowPoint B, Vector2 P)
    {
        Vector2 posA = A.position;
        Vector2 posB = B.position;

        Vector2 AP = P - posA;       //Vector from A to P   
        Vector2 AB = posB - posA;       //Vector from A to B  

        float distAB = AB.sqrMagnitude;    
        float ABAPproduct = Vector2.Dot(AP, AP);
        float distToLine = ABAPproduct / distAB; //The normalized "distance" from a to closest point  

        if (distToLine < 0) return posA;
        else if (distToLine > 1) return posB;
        else
        {
            Debug.DrawLine(P, posA + AB * distToLine);
            return posA + AB * distToLine;
        }
    }

    private void InitializePoints()
    {
        for (int i = 0; i < points.Length; i++)
        {
            FollowPoint currentPoint = points[i];
            FollowPoint nextPoint = i == points.Length - 1 ? null : points[i + 1];

            if (nextPoint == null)
            {
                currentPoint.nextPointDir = points[i - 1].nextPointDir;
                currentPoint.nextPointDistance = 0;
                break;
            }

            Debug.DrawLine(currentPoint.position, nextPoint.position, Color.green, 100000);

            Vector2 diff = (nextPoint.position - currentPoint.position);
            float magnitude = diff.magnitude;
            currentPoint.nextPointDir = diff / magnitude;
            currentPoint.nextPointDistance = magnitude;
        }
    }

    public Vector2 GetPointOnLevelLine(Vector2 P)
    {
        if (!pointArrayValid) return Vector2.zero;

        int bestIndex = 0;
        FollowPoint closestPoint = points[0];
        float closestDistance = float.MaxValue;
        Vector2 bestPos = closestPoint.position;

        for (int i = 0; i < points.Length; i++)
        {
            FollowPoint currentPoint = points[i];
            float dist = (currentPoint.position - P).sqrMagnitude;

            if(dist < closestDistance)
            {
                bestIndex = i;
                closestPoint = currentPoint;
                bestPos = closestPoint.position;
                closestDistance = dist;

                Debug.Log(closestPoint.gameObject.name);
            }
        }

        FollowPoint ahead = null;
        if(bestIndex < points.Length -1) ahead = points[bestIndex + 1];
        FollowPoint behind = null;
        if(bestIndex > 0 ) behind = points[bestIndex - 1];

        if(behind == null)
            GetClosestPointOnOneLine(closestPoint, ahead, P);
        if(ahead == null)
            return GetClosestPointOnOneLine(behind, closestPoint, P);

        float behindDot = Vector2.Dot(behind.nextPointDir, P - behind.position);
        float currentDot = Vector2.Dot(closestPoint.nextPointDir, P - closestPoint.position);

        if(behindDot > pointDotRange && currentDot < pointDotRange)
            return GetClosestPointOnOneLine(behind, closestPoint, P);

        Debug.DrawLine(bestPos, P, Color.green);
        return GetClosestPointOnOneLine(closestPoint, ahead, P);
    }
}
