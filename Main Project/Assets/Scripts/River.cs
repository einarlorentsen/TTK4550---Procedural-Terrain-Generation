using System.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using System.Drawing;
using UnityEditor;

struct ClosestPoint
{
    public int index;
    public float distance;
}

public struct SinWave
{
    public float amplitude;
    public float frequency;
}

public class River
{
    public Vector3 startPoint;
    public Vector3 endPoint;
    public SinWave firstSin;
    public SinWave secondSin;

    public int lakeMergeRadius = 10;
    public int mergeRadius;
    //public float riverDepth = 0.007f;
    public float riverDepth = 0.005f;
    public float width;

    public Vector3[] riverPoints;
    public List<Vector2> parentPoints = new List<Vector2>();
    public List<Vector2> parentMergePoints = new List<Vector2>();

    // For collisions with another river
    public bool mergeWithRiver = false;
    public int collisionIndex;
    public float collisionYValue;

    public River(Lake parent, Lake child, float[,] heightData, List<River> rivers, float riverWidth, int riverMergeRadius, System.Random rnd)
    {
        width = riverWidth;
        mergeRadius = riverMergeRadius;
        float angleBetweenLakes = CalculateAngle(parent, child);

        startPoint = GenerateEllipticEdgePoint(parent, angleBetweenLakes);
        endPoint = GenerateEllipticEdgePoint(child, angleBetweenLakes - Mathf.PI);
        SetSineWaveCoefficents(rnd);
        CreateRiverPoints();
        CalculateLakePoints(parent.lakeEdge, parentPoints);

        if(rivers.Count > 0)
        {
            mergeWithRiver = CollidesWithRiver(rivers, parent);
            if (mergeWithRiver)
            {
                Debug.Log("COLLIDING!");
                // Recalculate riverpoints with new collision endpoint
                CreateRiverPoints();
            }
        }

        SetRiverPointsHeight(parent, heightData);

        if (mergeWithRiver)
        {
            Debug.Log("SETTING COLLIDING MERGEHEIGHTS!");
            SetRiverMergeHeights(parent, child, heightData, rivers);
        }

        else
        {
            SetRiverHeights(parent, child, heightData);
        }
    }

    private void SetSineWaveCoefficents(System.Random rnd)
    {
        firstSin.amplitude = rnd.Next(8, 10);
        firstSin.frequency = rnd.Next(10, 12);

        secondSin.amplitude = rnd.Next(11, 13);
        secondSin.frequency = rnd.Next(15, 17);
    }

    // Set values of river in XY plane (plot the equation)
    private void CreateRiverPoints()
    {
        riverPoints = new Vector3[(int)Mathf.Abs(startPoint.x - endPoint.x)];
        Vector3 riverPoint = new Vector3();

        List<Vector3> riverPointsList = new List<Vector3>();

        if (startPoint.x < endPoint.x)
        {
            for (int x = (int)startPoint.x; x <= endPoint.x; x++)
            {
                riverPoint.x = x;
                riverPoint.z = CalculateYValue(x, startPoint, endPoint);
                riverPointsList.Add(riverPoint);
            }
        }

        else
        {
            for (int x = (int)startPoint.x; x >= endPoint.x; x--)
            {
                riverPoint.x = x;
                riverPoint.z = CalculateYValue(x, startPoint, endPoint);
                riverPointsList.Add(riverPoint);
            }
        }

        riverPoints = riverPointsList.ToArray();
    }

    // Calculates xy coordinates of lake edge
    private void CalculateLakePoints(Lake lake, List<Vector2> list)
    {
        float upperLakeValue, lowerLakeValue;
        for (int x = (int)lake.midpoint.x - lake.a; x <= (int)lake.midpoint.x + lake.a; x++)
        {
            upperLakeValue = lake.midpoint.z + Mathf.Sqrt(lake.b * lake.b - lake.b * lake.b * ((float)Mathf.Pow((x - lake.midpoint.x), 2f)) / (lake.a * lake.a));
            lowerLakeValue = lake.midpoint.z - Mathf.Sqrt(lake.b * lake.b - lake.b * lake.b * ((float)Mathf.Pow((x - lake.midpoint.x), 2f)) / (lake.a * lake.a));

            Vector2 upperPoint = new Vector2((float)x, upperLakeValue);
            Vector2 lowerPoint = new Vector2((float)x, lowerLakeValue);

            list.Add(upperPoint);
            list.Add(lowerPoint);
        }
    }

    private bool CollidesWithRiver(List<River> rivers, Lake parent)
    {
        Vector2 currentRiverPointVector;
        Vector2 otherRiverPointVector;
        float distance = 100f;
        int index = 0;
        foreach (var otherRiver in rivers)
        {
            // Iterate through all points in current riverpoints..
            for (int i = 0; i < riverPoints.Length; i++)
            {
                currentRiverPointVector = new Vector2(riverPoints[i].x, riverPoints[i].z);

                // .. And check against another rivers points
                for (int j = 0; j < otherRiver.riverPoints.Length; j++)
                {
                    otherRiverPointVector = new Vector2(otherRiver.riverPoints[j].x, otherRiver.riverPoints[j].z);
                    distance = Vector2.Distance(currentRiverPointVector, otherRiverPointVector);

                    // If we collide and the collisionpoint is lower than currentRivers startpoint
                    if(distance < otherRiver.width && otherRiver.riverPoints[j].y < parent.midpoint.y)
                    {
                        endPoint = otherRiver.riverPoints[j];
                        collisionIndex = index;
                        collisionYValue = otherRiver.riverPoints[j].y;
                        return true;
                    }
                }
            }
            index++;
        }
        return false;
    }

    // Sets height of river points (y-value)
    private void SetRiverPointsHeight(Lake parent, float[,] heightData)
    {

        float pointHeight, averageDescent;

        for (int i = 0; i < riverPoints.Length; i++)
        {
            int x = (int)riverPoints[i].x, y = (int)riverPoints[i].z;

            if (i == 0)
            {
                pointHeight = parent.midpoint.y;
                heightData[y, x] = pointHeight;
                riverPoints[i].y = pointHeight;
                continue;
            }

            int xPrevious = (int)riverPoints[i - 1].x, yPrevious = (int)riverPoints[i - 1].z;

            if (heightData[y, x] < heightData[yPrevious, xPrevious] && heightData[y, x] >= endPoint.y)
            {
                pointHeight = heightData[y, x];
            }

            else
            {
                Vector2 destination = new Vector2(endPoint.x, endPoint.z);
                Vector2 currentPoint = new Vector2(x, y);

                averageDescent = Mathf.Abs(endPoint.y - heightData[yPrevious, xPrevious]) / Vector2.Distance(destination, currentPoint);

                if (heightData[yPrevious, xPrevious] - averageDescent > endPoint.y)
                {
                    pointHeight = heightData[yPrevious, xPrevious] - averageDescent;
                }

                else
                {
                    pointHeight = heightData[yPrevious, xPrevious];
                }

            }

            heightData[y, x] = pointHeight;
            riverPoints[i].y = pointHeight;

        }
    }

    // Sets and merges river
    private void SetRiverHeights(Lake parent, Lake child, float[,] heightData)
    {
        int maxHeight = heightData.GetLength(0) - 1;
        int maxWidth = heightData.GetLength(1) - 1;
        int searchOffset = 100;

        float newHeight, startHeight, normalizedDistance;

        for (int x = (int)Mathf.Min(parent.midpoint.x, child.midpoint.x) - searchOffset; x < (int)Mathf.Max(parent.midpoint.x, child.midpoint.x) + searchOffset; x++)
        {
            for (int y = (int)Mathf.Min(parent.midpoint.z, child.midpoint.z) - searchOffset; y < (int)Mathf.Max(parent.midpoint.z, child.midpoint.z) + searchOffset; y++)
            {

                if (!OutOfBounds(y, x, maxHeight, maxWidth))
                {

                    float currentHeight = heightData[y, x];

                    float parentLake = Mathf.Pow(x - parent.midpoint.x, 2f) / Mathf.Pow(parent.a, 2f) + Mathf.Pow(y - parent.midpoint.z, 2f) / Mathf.Pow(parent.b, 2f);
                    float childLake = Mathf.Pow(x - child.midpoint.x, 2f) / Mathf.Pow(child.a, 2f) + Mathf.Pow(y - child.midpoint.z, 2f) / Mathf.Pow(child.b, 2f);
                    float parentLakeEdge = Mathf.Pow(x - parent.midpoint.x, 2f) / Mathf.Pow(parent.lakeEdge.a, 2f) + Mathf.Pow(y - parent.midpoint.z, 2f) / Mathf.Pow(parent.lakeEdge.b, 2f);

                    ClosestPoint closestRiverPoint = DistanceToRiverPoint(riverPoints, y, x);

                    // U-shape of river
                    if (closestRiverPoint.distance <= width && parentLake > 1 && childLake >= 1)
                    {
                        if(parentLakeEdge < 1)
                        {
                            newHeight = riverPoints[closestRiverPoint.index].y;
                            heightData[y, x] = newHeight;
                        }

                        else
                        {
                            normalizedDistance = Mathf.InverseLerp(0, width, closestRiverPoint.distance);
                            newHeight = riverPoints[closestRiverPoint.index].y + riverDepth * Mathf.Pow((normalizedDistance), 2f);
                            heightData[y, x] = newHeight;
                        }
                    }

                    // Merging with terrain
                    else if (closestRiverPoint.distance > width && closestRiverPoint.distance < mergeRadius && childLake >= 1 && parentLakeEdge > 1)
                    {
                        startHeight = riverPoints[closestRiverPoint.index].y + riverDepth;
                        newHeight = MergeHeight(currentHeight, width, mergeRadius, closestRiverPoint.distance, startHeight);
                        heightData[y, x] = newHeight;
                    }

                }
            }
        }
        MergeLake(parent, heightData, parentMergePoints);
    }

    private void SetRiverMergeHeights(Lake parent, Lake child, float[,] heightData, List<River> otherRivers)
    {
        int maxHeight = heightData.GetLength(0) - 1;
        int maxWidth = heightData.GetLength(1) - 1;
        int searchOffset = 100;
        width = width / 2;

        float newHeight, startHeight, normalizedDistance;

        for (int x = (int)Mathf.Min(parent.midpoint.x, child.midpoint.x) - searchOffset; x < (int)Mathf.Max(parent.midpoint.x, child.midpoint.x) + searchOffset; x++)
        {
            for (int y = (int)Mathf.Min(parent.midpoint.z, child.midpoint.z) - searchOffset; y < (int)Mathf.Max(parent.midpoint.z, child.midpoint.z) + searchOffset; y++)
            {
                if (!OutOfBounds(y, x, maxHeight, maxWidth))
                {

                    float currentHeight = heightData[y, x];

                    float parentLake = Mathf.Pow(x - parent.midpoint.x, 2f) / Mathf.Pow(parent.a, 2f) + Mathf.Pow(y - parent.midpoint.z, 2f) / Mathf.Pow(parent.b, 2f);
                    float childLake = Mathf.Pow(x - child.midpoint.x, 2f) / Mathf.Pow(child.a, 2f) + Mathf.Pow(y - child.midpoint.z, 2f) / Mathf.Pow(child.b, 2f);
                    float parentLakeEdge = Mathf.Pow(x - parent.midpoint.x, 2f) / Mathf.Pow(parent.lakeEdge.a, 2f) + Mathf.Pow(y - parent.midpoint.z, 2f) / Mathf.Pow(parent.lakeEdge.b, 2f);

                    ClosestPoint closestRiverPoint = DistanceToRiverPoint(riverPoints, y, x);
                    ClosestPoint closestOtherRiverPoint = DistanceToRiverPoint(otherRivers[collisionIndex].riverPoints, y, x);

                    // U-shape of river
                    if(closestOtherRiverPoint.distance < otherRivers[collisionIndex].width && closestRiverPoint.distance <= width && parentLake > 1 && childLake >= 1) // When river U-shape wants to be drawn inside otherRiver right before collision 
                    {
                        heightData[y, x] = collisionYValue;
                    }

                    else if (closestRiverPoint.distance <= width && parentLake > 1 && childLake >= 1)
                    {
                        if (parentLakeEdge < 1)
                        {
                            newHeight = riverPoints[closestRiverPoint.index].y;
                            heightData[y, x] = newHeight;
                        }

                        else
                        {
                            normalizedDistance = Mathf.InverseLerp(0, width, closestRiverPoint.distance);
                            newHeight = riverPoints[closestRiverPoint.index].y + riverDepth * Mathf.Pow((normalizedDistance), 2f);
                            heightData[y, x] = newHeight;
                        }
                    }

                    // Merging with terrain
                    else if (closestOtherRiverPoint.distance < otherRivers[collisionIndex].width) // Do not allow current river to merge over river it is colliding with
                    {
                        continue;
                    }

                    else if (closestRiverPoint.distance > width && closestRiverPoint.distance < mergeRadius && childLake >= 1 && parentLakeEdge > 1)
                    {
                        startHeight = riverPoints[closestRiverPoint.index].y + riverDepth;
                        newHeight = MergeHeight(currentHeight, width, mergeRadius, closestRiverPoint.distance, startHeight);
                        heightData[y, x] = newHeight;
                    }

                }
            }
        }
        MergeLake(parent, heightData, parentMergePoints);
    }

    private void MergeLake(Lake parent, float[,] heightData, List<Vector2> mergeZonePoints)
    {
        Lake mergeZone = new Lake(parent.midpoint, parent.lakeEdge.a, parent.lakeEdge.b, lakeMergeRadius);
        CalculateLakePoints(mergeZone, mergeZonePoints);

        int maxHeight = heightData.GetLength(0) - 1;
        int maxWidth = heightData.GetLength(1) - 1;

        float parentLakeEdge, mergeZoneEdge, distanceToLakeCenter, angle, radiusLakeEdge, radiusMergeZone, normalizedDistance, heightDifference, currentHeight;
        int mergeY, mergeX;
        ClosestPoint closestRiverPoint, closestMergeZonePoint;
        for (int x = (int)mergeZone.midpoint.x - mergeZone.a; x < (int)mergeZone.midpoint.x + mergeZone.a; x++)
        {
            for (int y = (int)mergeZone.midpoint.z - mergeZone.b; y < (int)mergeZone.midpoint.z + mergeZone.b; y++)
            {
                closestMergeZonePoint = DistanceToLakePoint(mergeZonePoints, y, x);
                mergeY = (int)mergeZonePoints[closestMergeZonePoint.index].y;
                mergeX = (int)mergeZonePoints[closestMergeZonePoint.index].x;

                if (!OutOfBounds(y, x, maxHeight, maxWidth) && !OutOfBounds(mergeY, mergeX, maxHeight, maxWidth))
                {
                    closestRiverPoint = DistanceToRiverPoint(riverPoints, y, x);
                    parentLakeEdge = Mathf.Pow(x - parent.midpoint.x, 2f) / Mathf.Pow(parent.lakeEdge.a, 2f) + Mathf.Pow(y - parent.midpoint.z, 2f) / Mathf.Pow(parent.lakeEdge.b, 2f);
                    mergeZoneEdge = Mathf.Pow(x - mergeZone.midpoint.x, 2f) / Mathf.Pow(mergeZone.a, 2f) + Mathf.Pow(y - mergeZone.midpoint.z, 2f) / Mathf.Pow(mergeZone.b, 2f);

                    if (parentLakeEdge >= 1 && mergeZoneEdge <= 1 && closestRiverPoint.distance > width)
                    {
                        distanceToLakeCenter = parent.DistanceToLakeCenter(parent, y, x);
                        angle = parent.CalculateAngle(parent, y, x);
                        radiusLakeEdge = CalculateRadius(parent.lakeEdge, angle);
                        radiusMergeZone = CalculateRadius(mergeZone, angle);

                        normalizedDistance = Mathf.InverseLerp(radiusLakeEdge, radiusMergeZone, distanceToLakeCenter);
                        currentHeight = heightData[mergeY, mergeX];
                        heightDifference = Mathf.Abs(currentHeight - (parent.midpoint.y + parent.lakeEdgeHeightExtra)) * Mathf.Exp(-(Mathf.Pow(1 - normalizedDistance, 2f)) / 0.20f);

                        if (currentHeight > parent.midpoint.y + parent.lakeEdgeHeightExtra)
                        {
                            heightData[y, x] = parent.midpoint.y + parent.lakeEdgeHeightExtra + heightDifference;
                        }
                        else
                        {
                            heightData[y, x] = parent.midpoint.y + parent.lakeEdgeHeightExtra - heightDifference;
                        }
                    }
                }
            }
        }
    }

    private float MergeHeight(float currentHeight, float startDistance, float stopDistance, float normalizeVariable, float startHeight)
    {
        float normalizedDistance = Mathf.InverseLerp(startDistance, stopDistance, normalizeVariable);
        float riverTerrainHeight = startHeight * Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.20f);
        float blendingFactorRiver = Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.15f);
        float blendingFactorMountain = -Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.15f) + 1f;
        float heightToAdd = currentHeight * blendingFactorMountain + riverTerrainHeight * blendingFactorRiver;
        return heightToAdd;
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

    private static ClosestPoint DistanceToLakePoint(List<Vector2> lakePoints, int y, int x)
    {
        ClosestPoint point;

        float distance = 100f;
        int index = 0;

        for (int i = 0; i < lakePoints.Count; i++)
        {
            Vector2 lakePoint = lakePoints[i];
            Vector2 currentPoint = new Vector2(x, y);
            float distanceToRiverPoint = Vector2.Distance(lakePoint, currentPoint);

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


    public float CalculateYValue(int x, Vector3 pointA, Vector3 pointB)
    {

        float slope = (pointB.z - pointA.z) / (pointB.x - pointA.x);
        //float YValue = (int)(slope * (x - pointA.x) + pointA.z) + 10f * Mathf.Sin(x / 10f) + 12f * Mathf.Sin(x / 15f);
        float YValue = (int)(slope * (x - pointA.x) + pointA.z) + firstSin.amplitude * Mathf.Sin(x / firstSin.frequency) + secondSin.amplitude * Mathf.Sin(x / secondSin.frequency);
        return YValue;
    }

    public float CalculateAngle(Lake parent, Lake child)
    {
        float adjacent = child.midpoint.x - parent.midpoint.x;
        float opposite = child.midpoint.z - parent.midpoint.z;
        float angle = Mathf.Atan2(opposite, adjacent);

        if (angle < 0)
        {
            angle += 2 * Mathf.PI;
        }

        return angle;
    }

    private float CalculateRadius(Lake lake, float angle)
    {
        float radius = (lake.a * lake.b) / Mathf.Sqrt(Mathf.Pow(lake.a * Mathf.Sin(angle), 2) + Mathf.Pow(lake.b * Mathf.Cos(angle), 2));
        return radius;
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

    private Vector3 GenerateEllipticEdgePoint(Lake lake, float angle)
    {
        float hypotenuse = CalculateRadius(lake, angle);
        float opposite = hypotenuse * Mathf.Sin(angle);
        float adjacent = hypotenuse * Mathf.Cos(angle);

        Vector3 edgePoint = new Vector3(lake.midpoint.x + adjacent, lake.midpoint.y, lake.midpoint.z + opposite);
        return edgePoint;
    }
}