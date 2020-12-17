using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mountain
{
    public Vector3 midpoint;
    public float radius;

    public Mountain(int height, int radiusMin, int radiusMax, float[,] heightData, System.Random rnd)
    {
        //Generate radius for mountain
        radius = rnd.Next(radiusMin, radiusMax);

        // Generate x0 and y0 for ellipse;
        int x0 = rnd.Next(0, heightData.GetLength(1));
        int z0 = rnd.Next(0, heightData.GetLength(0));

        float mountainHeight = (float)rnd.Next(0, height) / 1000.0f;

        midpoint = new Vector3(x0, mountainHeight, z0);
    }

    public float Distance(int y, int x)
    {
        Vector2 currentPoint = new Vector2(x, y);
        Vector2 mountainMidpoint = new Vector2(midpoint.x, midpoint.z);

        return Vector2.Distance(currentPoint, mountainMidpoint);
    }

    public float NormalizeDistance(float start, float end, float distance)
    {
        return (distance - start) / (end - start);
    }

    public float Height(float normalizedDistance)
    {
        float mountainHeight = midpoint.y;

        return mountainHeight * Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.25f);
    }
}
