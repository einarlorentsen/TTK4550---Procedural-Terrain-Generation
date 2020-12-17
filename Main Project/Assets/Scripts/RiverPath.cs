using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiverPath
{
    Vector2[] points;

    public RiverPath(List<Vector3> anchorPoints)
    {
        points = new Vector2[(anchorPoints.Count - 1) * 3 + 1];
        for (int i = 0; i < anchorPoints.Count; i++)
        {
            points[i * 3] = new Vector2(anchorPoints[i].x, anchorPoints[i].z);
        }
        AutoSetAllControlPoints();
    }

    public Vector2 this[int i]
    {
        get
        {
            return points[i];
        }
    }

    public int NumPoints
    {

        get
        {
            return points.Length;
        }
    }

    public int NumSegments
    {
        get
        {
            return points.Length / 3;
        }
    }

    public Vector2[] GetPointsInSegment(int i)
    {
        return new Vector2[] { points[i * 3], points[i * 3 + 1], points[i * 3 + 2], points[LoopIndex(i * 3 + 3)] };
    }

    private void AutoSetAllControlPoints()
    {
        for (int i = 0; i < points.Length; i += 3) // loop through anchorpoints
        {
            AutoSetAnchorControlPoints(i);
        }

        AutoSetStartAndEndControls();
    }

    private void AutoSetAnchorControlPoints(int anchorIndex)
    {
        Vector2 anchorPos = points[anchorIndex];
        Vector2 direction = Vector2.zero;
        float[] neighbourDistances = new float[3];

        if (anchorIndex - 3 >= 0) // first anchorpoint neighbour
        {
            Vector2 offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
            direction += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }
        if (anchorIndex + 3 >= 0) // second anchorpoint neighbour
        {
            Vector2 offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
            direction -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }

        direction.Normalize();

        for (int i = 0; i < 2; i++)
        {
            int controlIndex = anchorIndex + i * 2 - 1;
            if (controlIndex >= 0 && controlIndex < points.Length)
            {
                points[LoopIndex(controlIndex)] = anchorPos + direction * neighbourDistances[i] * 0.5f;
            }
        }
    }

    private void AutoSetStartAndEndControls()
    {
        points[1] = (points[0] + points[2]) * 0.5f;
        points[points.Length - 2] = (points[points.Length - 1] + points[points.Length - 3]) * 0.5f;
    }

    private int LoopIndex(int i)
    {
        return (i + points.Length) % points.Length; // adding points.Length to handle negative i values
    }
}