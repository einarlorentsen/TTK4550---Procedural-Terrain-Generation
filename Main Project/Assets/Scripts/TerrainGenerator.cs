using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    private System.Random rnd;
    private Vector3 terrainSize;
    private int heightMapResolution;

    private float[,] heightData;

    private List<Mountain> mountains = new List<Mountain>();
    private List<Lake> lakes = new List<Lake>();
    private List<River> rivers = new List<River>();

    public GameObject water;
    public GameObject water_river;
    public GameObject grass;
    public GameObject mushroom;
    public GameObject tree;

    /*------------------------------------------------------------------------------------*/

    [Header("Terrain")]
    public Terrain terrain;
    [Range(1, 1000)] public int numMountains = 100;
    [Range(0, 15)] public int numParentLakes = 1;
    [Range(0, 10)] public int numChildLakes = 3;
    [Range(0, 10)] public int numChildOfChildLakes = 0;

    [Header("Mountain")]

    [Range(1, 50)] public int radiusMinMountain = 50;
    [Range(1, 100)] public int radiusMaxMountain = 60;

    [Header("Lake")]

    [Range(0f, 150f)] public float maxDistanceToParent = 150f;
    [Range(1, 20)] public int radiusMinLakeParent = 20;
    [Range(1, 40)] public int radiusMaxLakeParent = 30;
    [Range(1, 10)] public int radiusMinLakeChild = 10;
    [Range(1, 20)] public int radiusMaxLakeChild = 15;
    [Range(1, 10)] public int radiusMinLakeBank = 7;
    [Range(1, 10)] public int radiusMaxLakeBank = 9;
    [Range(10, 100)] public int radiusMinSurroundingHeights = 80;
    [Range(10, 100)] public int radiusMaxSurroundingHeights = 90;


    // Start is called before the first frame update
    void Start()
    {
        InitializeTerrainVariables();
        
        for (int i = 0; i < numMountains; i++)
        {
            GenerateMountain(180);
        }
        
        for (int i = 0; i < numParentLakes; i++)
        {
            Lake parentLake = GenerateLakeParent(0.0f);
            for (int j = 0; j < numChildLakes; j++)
            {
                Lake childLake = GenerateLakeChild(0.05f, parentLake);
                for (int k = 0; k < numChildOfChildLakes; k++)
                {
                    Lake childchildLake = GenerateLakeChild(0.1f, childLake);
                    GenerateRiver(childLake, childchildLake);
                }
                
                GenerateRiver(parentLake, childLake);
            }
        }
        
        SetTerrainHeight();
    }

    private void InitializeTerrainVariables()
    {
        terrainSize = terrain.terrainData.size;
        rnd = new System.Random();

        heightMapResolution = terrain.terrainData.heightmapResolution;
        heightData = new float[heightMapResolution, heightMapResolution];
    }

    private void SetTerrainHeight()
    {
        for (int y = 0; y < heightMapResolution; y++)
        {
            for (int x = 0; x < heightMapResolution; x++)
            {
                heightData[y, x] = SetHeight(y, x);
            }
        }
        
        terrain.terrainData.SetHeights(0, 0, heightData);
        PlaceWater();
        PlaceRiver();
    }

    private float SetHeight(int y, int x)
    {
        float totalHeight = 0f;
        float totalBlending = 0f; float largestBlendingFactor = 0f; float lakeEdgeBlending = 0f;

        float closestDistanceLakeEdge = 100f; float closestDistanceRiverWidth = 100f;
        
        float lakeBankHeight = 0f; float insideRiverHeight = 0f;
        bool insideLakeBank = false; bool insideRiver = false;

        List<float> lakesHeight = new List<float>();
        List<float> riversHeight = new List<float>();

        foreach (Mountain mountain in mountains)
        {
            float distance = mountain.Distance(y, x);
            float normalizedDistance = mountain.NormalizeDistance(0, mountain.radius, distance);
            float mountainHeight = mountain.Height(normalizedDistance);

            totalHeight += mountainHeight;
        }
        
        foreach (Lake lake in lakes)
        {
            float distance = lake.Distance(y, x);

            if (distance <= lake.radius)
            {
                totalHeight = lake.midpoint.y;
                return totalHeight;
            }
            else if (distance <= lake.radiusLakeBank)
            {
                float normalizedDistance = lake.NormalizeDistance(lake.radius, lake.radiusLakeBank, distance);

                if (normalizedDistance <= closestDistanceLakeEdge)
                {
                    closestDistanceLakeEdge = normalizedDistance;
                    lakeBankHeight = lake.BankHeight(normalizedDistance);
                }
                insideLakeBank = true;
            }
            else
            {
                float normalizedDistance = lake.NormalizeDistance((float)lake.radiusLakeBank, (float)lake.radiusSurroundingHeight, distance);
                float blendingFactorLake = Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.15f);
                
                if (blendingFactorLake > largestBlendingFactor)
                {
                    largestBlendingFactor = blendingFactorLake;
                }

                float lakeHeight = Mathf.Pow(blendingFactorLake, 10f) * lake.SurroundingHeights(normalizedDistance);
                lakesHeight.Add(lakeHeight);
                totalBlending += Mathf.Pow(blendingFactorLake, 10f);
            }
        }
        
        foreach (River river in rivers)
        {
            Vector3 closestRiverPoint = river.ClosestRiverPoint(river.riverPoints, y, x);
            float distance = river.Distance(y, x, closestRiverPoint);

            if (distance <= river.widthRiver)
            {
                float normalizedDistance = river.NormalizeDistance(0, river.widthRiver, distance);

                if (normalizedDistance < closestDistanceRiverWidth)
                {
                    closestDistanceRiverWidth = normalizedDistance;
                    insideRiverHeight = river.RiverHeight(normalizedDistance, closestRiverPoint);
                }
                if (insideLakeBank) { lakeEdgeBlending = Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.15f); }
                insideRiver = true;
            }
            else
            {
                float normalizedDistance = river.NormalizeDistance(river.widthRiver, river.widthTerrain, distance);
                float blendingFactorRiver = Mathf.Exp(-(Mathf.Pow(normalizedDistance, 2f)) / 0.15f);

                if (blendingFactorRiver > largestBlendingFactor)
                {
                    largestBlendingFactor = blendingFactorRiver;
                }

                float riverHeight = Mathf.Pow(blendingFactorRiver, 10f) * river.SurroundingHeights(normalizedDistance, closestRiverPoint);
                riversHeight.Add(riverHeight);
                totalBlending += Mathf.Pow(blendingFactorRiver, 10f);
            }
        }

        if (insideRiver && insideLakeBank) { totalHeight = lakeEdgeBlending * insideRiverHeight + (1f - lakeEdgeBlending) * lakeBankHeight; }
        else if (insideRiver)
        {
            totalHeight = insideRiverHeight;
        }
        else if (insideLakeBank)
        {
            totalHeight = lakeBankHeight;
        }

        else
        {
            totalHeight *= (1f - largestBlendingFactor);
            
            if (totalBlending > 0)
            {
                for (int i = 0; i < lakesHeight.Count; i++)
                {
                    totalHeight += (largestBlendingFactor * lakesHeight[i] / totalBlending);
                }
                for (int i = 0; i < riversHeight.Count; i++)
                {
                    totalHeight += (largestBlendingFactor * riversHeight[i] / totalBlending);
                }
            }
            if (totalHeight <= 0.016f)
            {
                int chance = rnd.Next(0, 1000);
                if (chance < 690)
                {
                    int angle = rnd.Next(0, 180);

                    Vector3 position = new Vector3();
                    position.x = (terrainSize.x / heightMapResolution) * x;
                    position.y = terrainSize.y * totalHeight;
                    position.z = (terrainSize.z / heightMapResolution) * y;

                    if (chance <= 1)
                    {
                        Instantiate(tree, position, Quaternion.Euler(0, 90 - angle, 0));
                    }
                    else if (chance <= 6)
                    {
                        Instantiate(mushroom, position, Quaternion.Euler(0, 90 - angle, 0));
                    }
                    else
                    {
                        Instantiate(grass, position, Quaternion.Euler(0, 90 - angle, 0));
                    }
                }
            }
        }

        
        return totalHeight;
    }
    
    private void GenerateMountain(int height)
    {
        Mountain mountain = new Mountain(height, radiusMinMountain, radiusMaxMountain, heightData, rnd);
        mountains.Add(mountain);
    }

    private Lake GenerateLakeParent(float height)
    {
        int x0 = rnd.Next(0, heightData.GetLength(1));
        int z0 = rnd.Next(0, heightData.GetLength(0));

        Lake lake = new Lake(height, x0, z0, radiusMinLakeParent, radiusMaxLakeParent, radiusMinLakeBank, radiusMaxLakeBank, radiusMinSurroundingHeights, radiusMaxSurroundingHeights, rnd);
        lakes.Add(lake);
        return lake;
    }

    private Lake GenerateLakeChild(float height, Lake parentLake)
    {
        int x0; int z0;

        while (true)
        {
            x0 = rnd.Next(0, heightData.GetLength(1));
            z0 = rnd.Next(0, heightData.GetLength(0));
            if (parentLake.Distance(z0, x0) >= parentLake.radiusSurroundingHeight && parentLake.Distance(z0, x0) <= parentLake.radiusSurroundingHeight + maxDistanceToParent)
            {
                break;
            }
        }

        Lake lake = new Lake(height, x0, z0, radiusMinLakeChild, radiusMaxLakeChild, radiusMinLakeBank, radiusMaxLakeBank, radiusMinSurroundingHeights, radiusMaxSurroundingHeights, rnd);
        lakes.Add(lake);
        return lake;
    }

    private void GenerateRiver(Lake parent, Lake child)
    {
        River river = new River(parent, child, rivers, rnd);
        rivers.Add(river);
    }

    private void PlaceWater()
    {
        foreach (Lake lake in lakes)
        {
            Vector3 waterPosition = new Vector3();
            waterPosition.x = (terrainSize.x / heightMapResolution) * lake.midpoint.x;
            waterPosition.y = terrainSize.y * (lake.midpoint.y + lake.lakeBankHeightExtra / 1.6f);
            waterPosition.z = (terrainSize.z / heightMapResolution) * lake.midpoint.z;

            float assetScalingFactorX = (float)lake.radius/1.65f;
            float assetScalingFactorY = (float)lake.radius/1.65f;

            GameObject lakeWater = Instantiate(water, waterPosition, Quaternion.Euler(0, 90, 0));
            lakeWater.transform.localScale = new Vector3(assetScalingFactorX, 0.01f, assetScalingFactorY);
        }

    }

    private void PlaceRiver()
    {
        foreach (River river in rivers)
        {
            for (int i = 0; i < river.riverPoints.Length; i++)
            {
                Vector3 waterPosition = new Vector3();
                waterPosition.x = (terrainSize.x / heightMapResolution) * river.riverPoints[i].x;
                waterPosition.y = terrainSize.y * (river.riverPoints[i].y - river.riverDepth / 1.3f);
                waterPosition.z = (terrainSize.z / heightMapResolution) * river.riverPoints[i].z;

                float assetScalingFactorX = (float)river.widthRiver / 2f;
                float assetScalingFactorY = (float)river.widthRiver / 2f;

                GameObject riverWater = Instantiate(water_river, waterPosition, Quaternion.Euler(0, 90, 0));
                riverWater.transform.localScale = new Vector3(assetScalingFactorX, 0.01f, assetScalingFactorY);
            }
        }
    }
}