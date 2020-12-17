using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Road
{

    public Vector3 startPoint;
    public Vector3 endPoint;
    public Vector3[] roadPoints;

    public List<Vector3> bridgePoints = new List<Vector3>();

    public float width;
    public float roadEdgeWidth;
    public float roadEdgeHeight = 0.0025f;
    public float height;
    public int mergeRadius;


    public Road(float roadWidth, float edgeWidth, int roadMergeRadius, float[,] heightData, List<River> rivers, List<Road> roads, System.Random rnd)
    {
        width = roadWidth;
        roadEdgeWidth = edgeWidth;
        mergeRadius = roadMergeRadius;

        SetEdgePoints(heightData, rnd);
        CreateRoadPoints();
        SetRoadPointHeights(roads, rivers, rnd);
        DrawRoad(roads, rivers, heightData);
    }

    private void SetEdgePoints(float[,] heightData, System.Random rnd)
    {
        //int startX = rnd.Next(0, heightData.GetLength(1));
        //int startY = rnd.Next(0, heightData.GetLength(0));
        int startX = 80;
        int startY = 170;

        //int endX = rnd.Next(0, heightData.GetLength(1));
        //int endY = rnd.Next(0, heightData.GetLength(0));
        int endX = 470;
        int endY = 170;

        startPoint = new Vector3(startX, heightData[startY, startX], startY);
        endPoint = new Vector3(endX, heightData[endY, endX], endY);
    }

    // Fills roadpoints/ Plots road in the xy plane
    private void CreateRoadPoints()
    {
        roadPoints = new Vector3[(int)Mathf.Abs(startPoint.x - endPoint.x)];
        Vector3 roadPoint = new Vector3();

        List<Vector3> roadPointsList = new List<Vector3>();

        if (startPoint.x < endPoint.x)
        {
            for (int x = (int)startPoint.x; x <= endPoint.x; x++)
            {
                roadPoint.x = x;
                roadPoint.z = CalculateYValue(x, startPoint, endPoint);
                roadPointsList.Add(roadPoint);
            }
        }

        else
        {
            for (int x = (int)startPoint.x; x >= endPoint.x; x--)
            {
                roadPoint.x = x;
                roadPoint.z = CalculateYValue(x, startPoint, endPoint);
                roadPointsList.Add(roadPoint);
            }
        }

        roadPoints = roadPointsList.ToArray();
    }

    // Set y value/height of roadpoints
    private void SetRoadPointHeights(List<Road> roads, List<River> rivers, System.Random rnd)
    {
        height = 0.012f;
        //height = rnd.Next(1, 5) / 100f;

        foreach (Road road in roads)
        {
            for (int i = 0; i < roadPoints.Length; i++)
            {
                ClosestPoint closestOtherRoadPoint = DistanceToRoadPoint(road.roadPoints, (int)roadPoints[i].z, (int)roadPoints[i].x);
                if (closestOtherRoadPoint.distance <= road.width)
                {
                    height = road.height;
                    break;
                }
            }
        }

        foreach (River river in rivers)
        {
            for (int i = 0; i < roadPoints.Length; i++)
            {
                ClosestPoint closestRiverPoint = DistanceToRiverPoint(river.riverPoints, (int)roadPoints[i].z, (int)roadPoints[i].x);
                if(closestRiverPoint.distance <= river.width/6)
                {
                    height = river.riverPoints[closestRiverPoint.index].y + river.riverDepth;
                    Vector3 bridgePoint = new Vector3(roadPoints[i].x, height, roadPoints[i].z);
                    bridgePoints.Add(bridgePoint);
                    break;
                }
            }
        }

        startPoint.y = height;
        endPoint.y = height;

        for (int i = 0; i < roadPoints.Length; i++)
        {
            int x = (int)roadPoints[i].x, y = (int)roadPoints[i].z;
            roadPoints[i].y = height;
            //roadPoints[i].y = 0.01f  + 0.01f*Mathf.Sin(i/30f) + 0.005f*Mathf.Sin(i/20f);
        }
    }



    private void DrawRoad(List<Road> roads, List<River> rivers, float[,] heightData)
    {
        int maxHeight = heightData.GetLength(0) - 1;
        int maxWidth = heightData.GetLength(1) - 1;
        bool colliding = false;

        float newHeight, startHeight, normalizedDistance;
        for (int x = (int)Mathf.Min(startPoint.x, endPoint.x) - mergeRadius; x <= (int)Mathf.Max(startPoint.x, endPoint.x) + mergeRadius; x++)
        {
            for(int y = (int)Mathf.Min(startPoint.z, endPoint.z) - mergeRadius; y <= (int)Mathf.Max(startPoint.z, endPoint.z) + mergeRadius; y++)
            {
                if (!OutOfBounds(y, x, maxHeight, maxWidth))
                {
                    colliding = false;
                    float currentHeight = heightData[y, x];
                    ClosestPoint closestRoadPoint = DistanceToRoadPoint(roadPoints, y, x);

                    // Check if we are colliding with a road
                    foreach(Road road in roads)
                    {
                        ClosestPoint closestOtherRoadPoint = DistanceToRoadPoint(road.roadPoints, y, x);
                        if(closestOtherRoadPoint.distance <= road.width)
                        {
                            colliding = true;
                            break;
                        }
                    }

                    // Check if we are colliding with a river
                    foreach(River river in rivers)
                    {
                        ClosestPoint closestRiverPoint = DistanceToRiverPoint(river.riverPoints, y, x);
                        if(closestRiverPoint.distance <= river.width)
                        {
                            colliding = true;
                            break;
                        }
                    }

                    if (colliding)
                    {
                        continue;
                    }

                    // Place road
                    else if(closestRoadPoint.distance <= width)
                    {
                        heightData[y, x] = roadPoints[closestRoadPoint.index].y;
                    }

                    // Place road edge
                    else if(closestRoadPoint.distance <= width + roadEdgeWidth)
                    {
                        normalizedDistance = Mathf.InverseLerp(width, width + roadEdgeWidth, closestRoadPoint.distance);
                        newHeight = height - roadEdgeHeight * Mathf.Exp(-(Mathf.Pow(normalizedDistance - 0.5f, 2f)) / 0.05f);
                        heightData[y, x] = newHeight;
                    }

                    // Merge road
                    else if(closestRoadPoint.distance <= mergeRadius)
                    {
                        startHeight = height;
                        newHeight = MergeHeight(currentHeight, width + roadEdgeWidth, mergeRadius, closestRoadPoint.distance, startHeight);
                        heightData[y, x] = newHeight;
                    }
                }
            }
        }
    }

    private static ClosestPoint DistanceToRoadPoint(Vector3[] roadPoints, int y, int x)
    {
        ClosestPoint point;

        float distance = 100f;
        int index = 0;

        for (int i = 0; i < roadPoints.Length; i++)
        {
            Vector2 riverPoint = new Vector2(roadPoints[i].x, roadPoints[i].z);
            Vector2 currentPoint = new Vector2(x, y);
            float distanceToRiverPoint = Vector2.Distance(riverPoint, currentPoint);

            if (distanceToRiverPoint < distance)
            {
                distance = distanceToRiverPoint;
                index = i;
            }
        }

        point.index = index;
        point.distance = distance;

        return point;
    }

    private static ClosestPoint DistanceToRiverPoint(Vector3[] riverPoints, int y, int x)
    {
        ClosestPoint point;

        float distance = 100f;
        int index = 0;

        for (int i = 0; i < riverPoints.Length; i++)
        {
            Vector2 riverPoint = new Vector2(riverPoints[i].x, riverPoints[i].z);
            Vector2 currentPoint = new Vector2(x, y);
            float distanceToRiverPoint = Vector2.Distance(riverPoint, currentPoint);

            if (distanceToRiverPoint < distance)
            {
                distance = distanceToRiverPoint;
                index = i;
            }
        }

        point.index = index;
        point.distance = distance;

        return point;
    }


    private float MergeHeight(float currentHeight, float startDistance, float stopDistance, float normalizeVariable, float startHeight)
    {
        float normalizedDistance = Mathf.InverseLerp(startDistance, stopDistance, normalizeVariable);
        float roadTerrainHeight = startHeight * Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.20f);
        float blendingFactorRiver = Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.15f);
        float blendingFactorMountain = -Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.15f) + 1f;
        float heightToAdd = currentHeight * blendingFactorMountain + roadTerrainHeight * blendingFactorRiver;
        return heightToAdd;
    }

    // Calculates y value for input x
    private float CalculateYValue(int x, Vector3 pointA, Vector3 pointB)
    {

        float slope = (pointB.z - pointA.z) / (pointB.x - pointA.x);
        //float YValue = (int)(slope * (x - pointA.x) + pointA.z) + 4 * Mathf.Sin(x / 30f) + 6 * Mathf.Sin(x / 50f);
        float YValue = (int)(slope * (x - pointA.x) + pointA.z);
        return YValue;
    }

    private bool OutOfBounds(int y, int x, int maxHeight, int maxWidth)
    {
        bool outOfBounds = false;

        if (x < 0 || y < 0 || y > maxHeight || x > maxWidth)
        {
            outOfBounds = true;
        }

        return outOfBounds;
    }
}
