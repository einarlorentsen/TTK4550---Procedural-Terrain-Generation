using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lake
{
    // Midpoint is x0, height, z0
    public Vector3 midpoint;
    public int a;
    public int b;

    public float lakeEdgeHeightExtra = 0.01f;

    public Lake lakeEdge;
    public Lake surroundingHeights;
    public Lake mergeLakeArea;

    public Lake(float height, int eccentricity, int radiusMin, int radiusMax, float[,] heightData, System.Random rnd)
    {
        // Generate a and b for equation
        a = rnd.Next(radiusMin, radiusMax);
        b = rnd.Next(a - eccentricity, a + eccentricity);

        if (b < radiusMin) { b = radiusMin; }
        if (b > radiusMax) { b = radiusMax; }

        // Generate x0 and y0 for ellipse;
        int x0 = rnd.Next(0, heightData.GetLength(1));
        int z0 = rnd.Next(0, heightData.GetLength(0));
        //int x0 = 100;
        //int z0 = 120;

        midpoint = new Vector3(x0, height, z0);
    }

    public Lake(Vector3 Midpoint, int A, int B, int mergeRadius)
    {
        midpoint = Midpoint;
        a = A + mergeRadius;
        b = B + mergeRadius;
    }

    // This constructor is used for surroundingHeights
    public Lake(Lake lake, int radiusMin, int radiusMax, System.Random rnd)
    {
        a = rnd.Next(lake.a + radiusMin, lake.a + radiusMax);
        b = lake.b + (a - lake.a);

        midpoint = new Vector3(lake.midpoint.x, lake.midpoint.y, lake.midpoint.z);
    }

    // This constructor is used to generate Lake pairs
    public Lake(Lake lake, float depth, int maxDistance, int sizeDifference, float[,] heightData, System.Random rnd)
    {
        int maxHeight = heightData.GetLength(0) - 1;
        int maxWidth = heightData.GetLength(1) - 1;

        a = rnd.Next(lake.a, lake.a + sizeDifference);
        b = rnd.Next(lake.b, lake.b + sizeDifference);

        int x0, z0, minXPosition, maxXPosition, minYPosition, maxYPosition;
        float ellipseEquationSurroundingHeights;

        minXPosition = (int)lake.midpoint.x - lake.surroundingHeights.a - maxDistance;
        maxXPosition = (int)lake.midpoint.x + lake.surroundingHeights.a + maxDistance;

        minYPosition = (int)lake.midpoint.z - lake.surroundingHeights.b - maxDistance;
        maxYPosition = (int)lake.midpoint.z + lake.surroundingHeights.b + maxDistance;

        while (true)
        {
            x0 = rnd.Next(minXPosition, maxXPosition);
            z0 = rnd.Next(minYPosition, maxYPosition);
            //x0 = 390;
            //z0 = 120;

            ellipseEquationSurroundingHeights = Mathf.Pow(x0 - lake.midpoint.x, 2) / Mathf.Pow(lake.surroundingHeights.a, 2) + Mathf.Pow(z0 - lake.midpoint.z, 2) / Mathf.Pow(lake.surroundingHeights.b, 2);
            if (ellipseEquationSurroundingHeights > 1 && !OutOfBounds(z0, x0, maxHeight, maxWidth))
            {
                break;
            }
        }

        midpoint = new Vector3(x0, depth, z0);
    }


    public void DrawLake(Lake lake, float[,] heightData)
    {
        int maxHeight = heightData.GetLength(0) - 1;
        int maxWidth = heightData.GetLength(1) - 1;

        // Draw lake
        for (int y = ((int)lake.midpoint.z - lake.b); y < (int)lake.midpoint.z + lake.b; y++)
        {
            for (int x = ((int)lake.midpoint.x - lake.a); x < (int)lake.midpoint.x + lake.a; x++)
            {
                if (OutOfBounds(y, x, maxHeight, maxWidth))
                {
                    continue;
                }
                else
                {
                    SetLakeHeight(lake, heightData, y, x);
                }
            }
        }
    }

    private static void SetLakeHeight(Lake lake, float[,] heightData, int y, int x)
    {
        float ellipseEquation = Mathf.Pow(x - lake.midpoint.x, 2f) / Mathf.Pow(lake.a, 2f) + Mathf.Pow(y - lake.midpoint.z, 2f) / Mathf.Pow(lake.b, 2f);

        // If our point is within the generated 2D shape
        if (ellipseEquation <= 1)
        {
            heightData[y, x] = lake.midpoint.y;
        }
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

    public void DrawLakeEdge(Lake lake, float[,] heightData)
    {
        int maxHeight = heightData.GetLength(0) - 1;
        int maxWidth = heightData.GetLength(1) - 1;

        for (int y = (int)(lake.midpoint.z - lake.lakeEdge.b); y < (int)lake.midpoint.z + lake.lakeEdge.b; y++)
        {
            for (int x = (int)(lake.midpoint.x - lake.lakeEdge.a); x < (int)lake.midpoint.x + lake.lakeEdge.a; x++)
            {

                if (OutOfBounds(y, x, maxHeight, maxWidth))
                {
                    continue;
                }

                else
                {
                    SetEdgeHeight(lake, heightData, y, x);
                }
            }
        }
    }

    private void SetEdgeHeight(Lake lake, float[,] heightData, int y, int x)
    {
        float ellipseEquationLake = Mathf.Pow(x - lake.midpoint.x, 2f) / Mathf.Pow(lake.a, 2f) + Mathf.Pow(y - lake.midpoint.z, 2f) / Mathf.Pow(lake.b, 2f);
        float ellipseEquationLakeEdge = Mathf.Pow(x - lake.midpoint.x, 2f) / Mathf.Pow(lake.lakeEdge.a, 2f) + Mathf.Pow(y - lake.midpoint.z, 2f) / Mathf.Pow(lake.lakeEdge.b, 2f);

        if (ellipseEquationLakeEdge <= 1 && ellipseEquationLake >= 1)
        {
            float distanceToLakeCenter = DistanceToLakeCenter(lake, y, x);
            float angle = CalculateAngle(lake, y, x);

            float radiusLake = CalculateRadius(lake, angle);
            float radiusLakeEdge = CalculateRadius(lake.lakeEdge, angle);

            float normalizedDistance = Mathf.InverseLerp(radiusLake, radiusLakeEdge, distanceToLakeCenter);
            float heightToAdd = (lakeEdgeHeightExtra) * Mathf.Pow((normalizedDistance), 2f);

            heightData[y, x] = lake.midpoint.y + heightToAdd;
        }
    }

    public void DrawSurroundingHeights(Lake lake, float[,] heightData)
    {
        int maxHeight = heightData.GetLength(0) - 1;
        int maxWidth = heightData.GetLength(1) - 1;

        for (int y = ((int)lake.midpoint.z - lake.surroundingHeights.b); y < ((int)lake.midpoint.z + lake.surroundingHeights.b); y++)
        {
            for (int x = ((int)lake.midpoint.x - lake.surroundingHeights.a); x < ((int)lake.midpoint.x + lake.surroundingHeights.a); x++)
            {

                if (OutOfBounds(y, x, maxHeight, maxWidth))
                {
                    continue;
                }

                else
                {
                    SetHeightOutsideLake(lake, heightData, y, x);
                }
            }
        }
    }

    private void SetHeightOutsideLake(Lake lake, float[,] heightData, int y, int x)
    {
        float ellipseEquationLakeEdge = Mathf.Pow(x - lake.lakeEdge.midpoint.x, 2) / Mathf.Pow(lake.lakeEdge.a, 2) + Mathf.Pow(y - lake.lakeEdge.midpoint.z, 2) / Mathf.Pow(lake.lakeEdge.b, 2);
        float ellipseEquationSurroundingHeights = Mathf.Pow(x - lake.surroundingHeights.midpoint.x, 2) / Mathf.Pow(lake.surroundingHeights.a, 2) + Mathf.Pow(y - lake.surroundingHeights.midpoint.z, 2) / Mathf.Pow(lake.surroundingHeights.b, 2);

        // Check if we are outside of the lake, but inside the surrounding heights
        if (ellipseEquationLakeEdge >= 1 && ellipseEquationSurroundingHeights <= 1)
        {
            MergeHeights(lake, heightData, y, x);
        }
    }

    private void MergeHeights(Lake lake, float[,] heightData, int y, int x)
    {
        float distanceToLakeCenter = DistanceToLakeCenter(lake, y, x);
        float angle = CalculateAngle(lake, y, x);

        float radiusLakeEdge = CalculateRadius(lake.lakeEdge, angle);
        float radiusSurroundingHeights = CalculateRadius(lake.surroundingHeights, angle);

        float normalizedDistance = Mathf.InverseLerp(radiusLakeEdge, radiusSurroundingHeights, distanceToLakeCenter);

        float lakeEdgeHeight = lake.midpoint.y + lakeEdgeHeightExtra;

        //float lakeTerrainHeight = -(lakeEdgeHeight) * Mathf.Pow(normalizedDistance, 2f) + lakeEdgeHeight;
        float lakeTerrainHeight = lakeEdgeHeight * Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.20f);

        float blendingFactorLake = Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.15f);
        float blendingFactorMountain = -Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.15f) + 1f;

        float currentHeight = heightData[y, x];

        heightData[y, x] = currentHeight * blendingFactorMountain + lakeTerrainHeight * blendingFactorLake;
        //heightData[y, x] = lakeTerrainHeight;
    }

    public float DistanceToLakeCenter(Lake lake, int y, int x)
    {
        Vector2 middlePoint = new Vector2(lake.midpoint.x, lake.midpoint.z);
        Vector2 currentPoint = new Vector2(x, y);
        float distanceToMiddlePoint = Vector2.Distance(middlePoint, currentPoint);
        return distanceToMiddlePoint;
    }

    public float CalculateAngle(Lake lake, int y, int x)
    {
        float adjacent = x - lake.midpoint.x;
        float opposite = y - lake.midpoint.z;
        float angle = Mathf.Atan2(opposite, adjacent);

        if (angle < 0)
        {
            angle += 2 * Mathf.PI;
        }

        return angle;
    }

    private float CalculateHeight(Lake lake, float normalizedDistance)
    {
        float height = lake.midpoint.y + lakeEdgeHeightExtra; //add the height added by shallowWater
        float heightToAdd = -(height) * Mathf.Pow(normalizedDistance, 2f) + height;

        return heightToAdd;
    }

    private float CalculateRadius(Lake lake, float angle)
    {
        float radius = (lake.a * lake.b) / Mathf.Sqrt(Mathf.Pow(lake.a * Mathf.Sin(angle), 2) + Mathf.Pow(lake.b * Mathf.Cos(angle), 2));
        return radius;
    }
}