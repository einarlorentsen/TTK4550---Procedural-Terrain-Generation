using System.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using System.Drawing;
using UnityEditor;

struct QuadrantSearch
{
    public bool zSearch;

    public bool positiveZ;
    public bool positiveX;

    public int searchMin;
    public int searchMax;
    public int iterateMin;
    public int iterateMax;

    public int searchDirection;
}

public class River
{
    public RiverPath path;

    public float angleBetweenLakes;
    int anchorPointInterval = 20;
    int bezierPointsPerSegment = 20;
    int anchorPointSetDistance = 10;
    int connectToOtherRiverDistance = 20;

    public Vector3 childStartPoint;
    public Vector3 parentEndPoint;
    Lake parentLake; Lake childLake;

    public float riverDepth = 0.01f;
    public float widthRiver = 6f;
    public float widthTerrain = 70f;

    public List<Vector3> anchorPoints;
    public Vector3[] riverPoints;

    private List<River> rivers;

    public River()
    {
        // Empty initializer
    }

    public River(Lake parent, Lake child, List<River> generatedRivers, System.Random rnd)
    {
        parentLake = parent; childLake = child;
        rivers = generatedRivers;

        angleBetweenLakes = CalculateAngle(parent, child);

        childStartPoint = GenerateRiverFlowPoint(child, angleBetweenLakes);
        parentEndPoint = GenerateRiverFlowPoint(parent, angleBetweenLakes - Mathf.PI);

        FindAnchorPoints(parent, child, rnd);

        path = new RiverPath(anchorPoints);

        CreateRiverPoints();
        SetRiverPointHeight();
    }

    private void FindAnchorPoints(Lake parent, Lake child,System.Random rnd)
    {
        QuadrantSearch value = GetQuadrantSearchParameters();

        anchorPoints = new List<Vector3>
        {
            childStartPoint
        };

        for (int anchorPointSearch = value.searchMin + (value.searchDirection*anchorPointInterval); value.searchMin + Math.Abs(anchorPointSearch - value.searchMin) < value.searchMin + Math.Abs(value.searchMax - value.searchMin); anchorPointSearch += value.searchDirection*anchorPointInterval) // +- searchDirection to not go through first and last point
        {
            if (Math.Abs(value.searchMax - anchorPointSearch) >= anchorPointInterval)
            {
                float dx = value.iterateMax - value.iterateMin;
                float t = (float)(value.searchMin - anchorPointSearch) / (float)(value.searchMin - value.searchMax);
                int average = (int)value.iterateMin + (int) (t * dx);
                
                int iteratePoint = rnd.Next(average - anchorPointSetDistance, average + anchorPointSetDistance);

                int x = (value.zSearch) ? iteratePoint : anchorPointSearch;
                int z = (value.zSearch) ? anchorPointSearch : iteratePoint;

                Vector3 currentPoint = new Vector3(x, 0, z);

                if (rivers.Count > 0)
                {
                    foreach (River river in rivers)
                    {
                        Vector3 closestRiverPoint = river.ClosestRiverPoint(river.riverPoints, z, x);
                        float distance = river.Distance(z, x, closestRiverPoint);

                        if (distance <= connectToOtherRiverDistance)
                        {
                            Debug.Log("COLLIDING!");
                            anchorPoints.Add(new Vector3(closestRiverPoint.x, closestRiverPoint.y, closestRiverPoint.z));
                            return;
                        }
                    }
                }
                anchorPoints.Add(currentPoint);
            }
        }
        anchorPoints.Add(parentEndPoint);
    }

    private void CreateRiverPoints()
    {
        riverPoints = new Vector3[(path.NumSegments * (bezierPointsPerSegment - 1)) + 1];

        for (int i = 0; i < path.NumSegments; i++)
        {
            Vector2[] points = path.GetPointsInSegment(i);
            Vector3[] bezierPoints = Handles.MakeBezierPoints(points[0], points[3], points[1], points[2], bezierPointsPerSegment);
            for (int j = 0; j < bezierPoints.Length - 1; j++)
            {
                riverPoints[(i * (bezierPoints.Length - 1)) + j] = new Vector3(bezierPoints[j].x, bezierPoints[j].z, bezierPoints[j].y);
            }
            if (i == path.NumSegments - 1)
            {
                int lastIndex = (i * (bezierPoints.Length - 1)) + (bezierPoints.Length - 1);
                riverPoints[lastIndex] = new Vector3(bezierPoints[bezierPoints.Length - 1].x, bezierPoints[bezierPoints.Length - 1].z, bezierPoints[bezierPoints.Length - 1].y);
            }
        }
    }

    private void SetRiverPointHeight()
    {
        for (int i = 0; i < riverPoints.Length; i++)
        {
            float dz = (anchorPoints[anchorPoints.Count - 1].y) - (anchorPoints[0].y);
            float t = ((float)i / (float)(riverPoints.Length - 1));
            float averageDescent = anchorPoints[0].y + t * dz;
            riverPoints[i].y = averageDescent;
        }
    }
    
    public float CalculateAngle(Lake parent, Lake child)
    {
        float adjacent = parent.midpoint.x - child.midpoint.x;
        float opposite = parent.midpoint.z - child.midpoint.z;
        float angle = Mathf.Atan2(opposite, adjacent);

        if (angle < 0)
        {
            angle += 2 * Mathf.PI;
        }

        return angle;
    }

    private Vector3 GenerateRiverFlowPoint(Lake lake, float angle)
    {
        float radius = lake.radiusLakeBank;
        float x = radius * Mathf.Cos(angle);
        float z = radius * Mathf.Sin(angle);

        Vector3 flowPoint = new Vector3(lake.midpoint.x + x, lake.midpoint.y + lake.lakeBankHeightExtra, lake.midpoint.z + z);
        return flowPoint;
    }
    
    public float NormalizeDistance(float start, float end, float distance)
    {
        return (distance - start) / (end - start);
    }

    public float Distance(int y, int x, Vector3 point)
    {
        Vector2 currentPoint = new Vector2(x, y);
        Vector2 riverPoint = new Vector2(point.x, point.z);

        return Vector2.Distance(currentPoint, riverPoint);
    }

    public Vector3 ClosestRiverPoint(Vector3[] riverPoints, int y, int x)
    {
        Vector3 closestRiverPoint = new Vector3();
        float distance = 100f;

        for (int i = 0; i < riverPoints.Length; i++)
        {
            float distanceToRiverPoint = Distance(y, x, riverPoints[i]);

            if (distanceToRiverPoint < distance)
            {
                distance = distanceToRiverPoint;
                closestRiverPoint = riverPoints[i];
            }
        }

        return closestRiverPoint;
    }

    public float RiverHeight(float normalizedDistance, Vector3 closestRiverPoint)
    {
        float riverHeight = closestRiverPoint.y - riverDepth * Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2)) / 0.2f);
        return riverHeight;
    }

    public float SurroundingHeights(float normalizedDistance, Vector3 closestRiverPoint)
    {
        float surroundingHeights = riverDepth * normalizedDistance + closestRiverPoint.y;
        return surroundingHeights;
    }


    private QuadrantSearch GetQuadrantSearchParameters()
    {
        QuadrantSearch value;

        bool zSearch = ((angleBetweenLakes <= (3f * Mathf.PI) / 4f) && (angleBetweenLakes >= Mathf.PI / 4f)) || ((angleBetweenLakes >= (5f * Mathf.PI) / 4f) && (angleBetweenLakes <= (7f * Mathf.PI) / 4f));
        bool positiveZ = angleBetweenLakes >= 0 && angleBetweenLakes <= Mathf.PI;
        bool positiveX = angleBetweenLakes >= (3f * Mathf.PI) / 2f || angleBetweenLakes <= Mathf.PI / 2f;

        value.zSearch = zSearch;
        value.positiveX = positiveX;
        value.positiveZ = positiveZ;

        value.searchMin = (zSearch) ? (int)childStartPoint.z : (int)childStartPoint.x;
        value.searchMax = (zSearch) ? (int)parentEndPoint.z : (int)parentEndPoint.x;
        value.iterateMin = (!zSearch) ? (int)childStartPoint.z : (int)childStartPoint.x;
        value.iterateMax = (!zSearch) ? (int)parentEndPoint.z : (int)parentEndPoint.x;

        int searchDirection;

        if (zSearch)
        {
            searchDirection = (positiveZ) ? 1 : -1;
        }
        else
        {
            searchDirection = (positiveX) ? 1 : -1;
        }

        value.searchDirection = searchDirection;

        return value;
    }
}