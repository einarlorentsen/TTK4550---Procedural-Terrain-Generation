using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mountain
{
    // Midpoint is x0, height, z0
    public Vector3 midpoint;
    public float a;
    public float b;
    public float exponential;

    public Mountain(float height, int eccentricity, int radiusMin, int radiusMax, float[,] heightData, System.Random rnd)
    {
        // Generate a and b for equation
        a = rnd.Next(radiusMin, radiusMax);
        b = rnd.Next((int)a - eccentricity, (int)a + eccentricity);
        if (b < radiusMin) { b = radiusMin; }
        if (b > radiusMax) { b = radiusMax; }

        // Generate x0 and y0 for ellipse;
        int x0 = rnd.Next(0, heightData.GetLength(1));
        int z0 = rnd.Next(0, heightData.GetLength(0));

        midpoint = new Vector3(x0, height, z0);

        // Generate exponential to make height profile
        exponential = rnd.Next(190, 350) / 100;
        exponential = 2f;
    }

    public void DrawMountain(Mountain mountain, float[,] heightData)
    {
        // Defines a confined search area
        for (int y = Convert.ToInt32(mountain.midpoint.z - mountain.b); y < mountain.midpoint.z + mountain.b; y++)
        {
            for(int x = Convert.ToInt32(mountain.midpoint.x - mountain.a); x < mountain.midpoint.x + mountain.a; x++)
            {
                if (x < 0) { x = 0; }
                if (y < 0) { y = 0; }
                if (y > heightData.GetLength(0) - 1) { break; }
                if (x > heightData.GetLength(1) - 1) { break; }

                // float circleEquation = (mountain.a * x - mountain.midpoint.x) * (mountain.a * x - mountain.midpoint.x) + (mountain.b * y - mountain.midpoint.z) * (mountain.b * y - mountain.midpoint.z);
                float ellipseEquation = Mathf.Pow(x - mountain.midpoint.x, 2) / Mathf.Pow(mountain.a, 2) + Mathf.Pow(y - mountain.midpoint.z, 2) / Mathf.Pow(mountain.b, 2);

                // If our point is within the generated 2D shape
                if (ellipseEquation <= 1)
                {
                    float currentHeight;

                    if (x == (int)mountain.midpoint.x && y == (int)mountain.midpoint.z)
                    {
                        currentHeight = heightData[y, x];
                        heightData[y, x] = currentHeight + mountain.midpoint.y;
                    }
                    else
                    {
                        float distanceToMiddlePoint = CalculateDistanceToMiddlePoint(mountain, y, x);
                        float angle = CalculateAngle(mountain, y, x);
                        float radius = CalculateRadius(mountain, angle);
                        float heightToAdd = CalculateHeight(distanceToMiddlePoint, radius, mountain, angle);
                        currentHeight = heightData[y, x];
                        heightData[y, x] = currentHeight + heightToAdd;
                    }
                   
                }

            }
        }
    }

    

    private static float CalculateDistanceToMiddlePoint(Mountain mountain, int y, int x)
    {
        Vector2 middlePoint = new Vector2(mountain.midpoint.x, mountain.midpoint.z);
        Vector2 currentPoint = new Vector2(x, y);
        float distanceToMiddlePoint = Vector2.Distance(middlePoint, currentPoint);
        return distanceToMiddlePoint;
    }

    float CalculateHeight(float distanceToTarget, float radius, Mountain mountain, float angle)
    {
        float normalizedDistance = Mathf.InverseLerp(0, radius, distanceToTarget);

        // Gaussian equation on format: height * e^((distance)^exponential) / 0.25
        // The exponential decides the slope form of the equation
        float heightToAdd = mountain.midpoint.y * Mathf.Exp(-(Mathf.Pow(normalizedDistance, mountain.exponential)) / 0.25f);

        return heightToAdd;
    }

    private float CalculateRadius(Mountain mountain, float angle)
    {
        float radius = (mountain.a * mountain.b) / Mathf.Sqrt(Mathf.Pow(mountain.a * Mathf.Sin(angle), 2) + Mathf.Pow(mountain.b * Mathf.Cos(angle), 2));
        return radius;
    }

    private float CalculateAngle(Mountain mountain, int y, int x)
    {
        float adjacent = Mathf.Abs(x - mountain.midpoint.x);
        float opposite = Mathf.Abs(y - mountain.midpoint.z);
        double angle = Math.Atan(opposite / adjacent);
        return (float)angle;
    }

}