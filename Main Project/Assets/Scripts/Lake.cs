using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lake
{
    public Vector3 midpoint;
    public int radius;
    public int radiusLakeBank;
    public int radiusSurroundingHeight;

    public float lakeBankHeightExtra = 0.01f;

    public Lake(float height, int x0, int z0, int radiusMinLake, int radiusMaxLake, int radiusMinLakeBank, int radiusMaxLakeBank, int radiusMinSurroundingHeight, int radiusMaxSurroundingHeights, System.Random rnd)
    {
        radius = rnd.Next(radiusMinLake, radiusMaxLake);
        radiusLakeBank = rnd.Next(radius + radiusMinLakeBank, radius + radiusMaxLakeBank);
        radiusSurroundingHeight = rnd.Next(radiusLakeBank + radiusMinSurroundingHeight, radiusLakeBank + radiusMaxSurroundingHeights);
        
        midpoint = new Vector3(x0, height, z0);
    }

    public float Distance(int y, int x)
    {
        Vector2 currentPoint = new Vector2(x, y);
        Vector2 lakeMidpoint = new Vector2(midpoint.x, midpoint.z);

        return Vector2.Distance(currentPoint, lakeMidpoint);
    }

    public float NormalizeDistance(float start, float end, float distance)
    {
        return (distance - start) / (end - start);
    }

    public float BankHeight(float normalizedDistance)
    {
        float bankHeight = (lakeBankHeightExtra) * Mathf.Pow((normalizedDistance), 2f) + midpoint.y;

        return bankHeight;
    }

    public float SurroundingHeights(float normalizedDistance)
    {
        float lakeTerrainHeight = (midpoint.y + lakeBankHeightExtra) * Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.20f);

        return lakeTerrainHeight;
    }
}