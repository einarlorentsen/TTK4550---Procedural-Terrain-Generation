using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    private System.Random rnd;
    private Vector3 terrainSize;
    private int heightMapResolution;

    private float[,] heightData;

    public GameObject startHouse;
    public GameObject endHouse;
    public GameObject bridge;
    public GameObject[] vegetation;
    public GameObject[] trees;
    public GameObject water;
    public GameObject riverWater;
    public GameObject[] waterLilly;
    public GameObject road;


    private List<Mountain> mountains = new List<Mountain>();
    private List<Lake> lakes = new List<Lake>();
    private List<List<Lake>> lakePairs = new List<List<Lake>>();
    public List<River> rivers = new List<River>();
    public List<Road> roads = new List<Road>();
    /*------------------------------------------------------------------------------------*/

    [Header("Terrain")]
    public Terrain terrain;
    [Range(0, 1)] public float baseHeight = 0.0f;

    [Header("TerrainGenerator")]
    [Range(0, 150)] public int largeMountains = 90;
    [Range(0, 1000)] public int smallMountains = 750;
    [Range(0, 10)] public int numLakes = 1;
    [Range(0, 5)] public int numRivers = 1;
    [Range(0, 5)] public int numRoads = 1;
    [Range(0, 50)] public int numTrees = 0;
    [Range(0, 500)] public int vegetationRadius = 200;
    [Range(0, 10)] public int numWaterLillys = 2;

    [Header("Mountain")]

    [Range(1, 30)] public int eccentricityMountain = 3;
    [Range(1, 20)] public int radiusMinMountain = 10; // NB! Is multiplied with 100 and then divided to get float value
    [Range(1, 100)] public int radiusMaxMountain = 30; // NB! Is multiplied with 100 and then divided to get float value


    [Header("Lake")]

    [Range(1, 30)] public int eccentricityLake = 15;
    [Range(1, 20)] public int radiusMinLake = 10;
    [Range(1, 40)] public int radiusMaxLake = 40;
    [Range(1, 20)] public int lakeEdgeMinWidth = 7;
    [Range(1, 30)] public int lakeEdgeMaxWidth = 9;
    [Range(10, 100)] public int surroundingHeightsMinWidth = 20;
    [Range(10, 100)] public int surroundingHeightsMaxWidth = 30;

    [Header("Lake Pairs")]
    [Range(80, 200)] public int maxDistance = 120;
    [Range(10, 50)] public int sizeDifference = 20;

    [Header("River")]
    [Range(1, 10)] public float riverWidth = 5f;
    [Range(10, 100)] public int riverMergeRadius = 40;

    [Header("Road")]
    [Range(1, 10)] public float roadWidth = 6f;
    [Range(1, 10)] public float roadEdgeWidth = 4f;
    [Range(10, 100)] public int roadMergeRadius = 30;

    private void Start()
    {
        InitializeTerrainVariables();

        

        for (int i = 0; i < largeMountains; i++)
        {
            GenerateMountain(0.04f);
        }

        for (int i = 0; i < smallMountains; i++)
        {
            GenerateMountain(0.004f);
        }

        for (int i = 0; i < numLakes; i++)
        {
            GenerateLake(0.01f);
        }

        
        
        
        for (int i = 0; i < numRivers; i++)
        {
            GenerateLakePair(lakes[i], 0.001f, heightData, rnd);
        }

        GenerateRiver(lakePairs[0][0], lakePairs[0][1], heightData, rnd);
        
        
        
        for(int i = 0; i < numRoads; i++){
            GenerateRoad();
        }

        PlaceRoad();
        PlaceHouses();
        PlaceBridges();
        PlaceWater(rnd);


        //GenerateRiver(lakePairs[1][0], lakePairs[1][1], heightData, rnd);

        //PlaceVegetation(heightData, rnd);
        PlaceVegetationWorld(heightData, lakes);

        terrain.terrainData.SetHeights(0, 0, heightData);

    }

    private void InitializeTerrainVariables()
    {
        terrainSize = terrain.terrainData.size;
        Vector3 terrainPosition = terrain.GetPosition();
        heightMapResolution = terrain.terrainData.heightmapResolution;

        // The samples are represented as float values ranging from 0 to 1. The array has the dimensions [height,width] and is indexed as [y,x]
        heightData = new float[heightMapResolution, heightMapResolution];

        ResetTerrain();

        rnd = new System.Random();

    }

    private void ResetTerrain()
    {
        for (int y = 0; y < heightMapResolution; y++)
        {
            for (int x = 0; x < heightMapResolution; x++)
            {
                heightData[y, x] = baseHeight;
            }
        }
    }

    private void GenerateLake(float depth)
    {
        Lake lake = new Lake(depth, eccentricityLake, radiusMinLake, radiusMaxLake, heightData, rnd);

        Lake lakeEdge = new Lake(lake, lakeEdgeMinWidth, lakeEdgeMaxWidth, rnd);
        lake.lakeEdge = lakeEdge;

        Lake surroundingHeights = new Lake(lake.lakeEdge, surroundingHeightsMinWidth, surroundingHeightsMaxWidth, rnd);
        lake.surroundingHeights = surroundingHeights;

        lakes.Add(lake);

        lake.DrawLake(lake, heightData);
        lake.DrawLakeEdge(lake, heightData);
        lake.DrawSurroundingHeights(lake, heightData);
    }

    private void GenerateLakePair(Lake lake, float depth, float[,] heightData, System.Random rnd)
    {
        Lake secondLake = new Lake(lake, depth, maxDistance, sizeDifference, heightData, rnd);

        Lake lakeEdge = new Lake(secondLake, lakeEdgeMinWidth, lakeEdgeMaxWidth, rnd);
        secondLake.lakeEdge = lakeEdge;

        Lake surroundingHeights = new Lake(secondLake.lakeEdge, surroundingHeightsMinWidth, surroundingHeightsMaxWidth, rnd);
        secondLake.surroundingHeights = surroundingHeights;

        lakes.Add(secondLake);
        lakePairs.Add(new List<Lake> { lake, secondLake });

        secondLake.DrawLake(secondLake, heightData);
        secondLake.DrawLakeEdge(secondLake, heightData);
        secondLake.DrawSurroundingHeights(secondLake, heightData);
    }



    private void GenerateMountain(float height)
    {
        Mountain mountain = new Mountain(height, eccentricityMountain, radiusMinMountain, radiusMaxMountain, heightData, rnd);

        mountains.Add(mountain);

        mountain.DrawMountain(mountain, heightData);
    }

    private void GenerateRiver(Lake parent, Lake child, float[,] heightMap, System.Random rnd)
    {
        River river = new River(parent, child, heightMap, rivers, riverWidth, riverMergeRadius ,rnd);
        rivers.Add(river);
    }

    private void GenerateRoad()
    {
        Road road = new Road(roadWidth, roadEdgeWidth, roadMergeRadius, heightData, rivers, roads, rnd);
        roads.Add(road);
    }

    private void PlaceHouses()
    {
        foreach(Road road in roads)
        {
            Vector3 startHousePosition = new Vector3();
            Vector3 endHousePosition = new Vector3();

            Vector2 start = new Vector2(road.startPoint.x, road.startPoint.z);
            Vector2 end = new Vector2(road.endPoint.x, road.endPoint.z);

            startHousePosition.x = (terrainSize.x / heightMapResolution) * road.startPoint.x;
            startHousePosition.y = terrainSize.y * road.endPoint.y;
            startHousePosition.z = (terrainSize.z / heightMapResolution) * road.startPoint.z;

            endHousePosition.x = (terrainSize.x / heightMapResolution) * road.endPoint.x;
            endHousePosition.y = terrainSize.y * road.endPoint.y;
            endHousePosition.z = (terrainSize.z / heightMapResolution) * road.endPoint.z;

            float angle = CalculateAngle(start, end) * 180 / Mathf.PI;
            if (Mathf.Abs(angle) > 180)
            {
                angle = angle % 180;
            }

            Instantiate(startHouse, startHousePosition, Quaternion.Euler(0, 90 - angle, 0));
            Instantiate(endHouse, endHousePosition, Quaternion.Euler(0, 90 - angle, 0));
        }
    }

    private void PlaceBridges()
    {
        foreach(Road road in roads)
        {
            foreach (Vector3 bridgePoint in road.bridgePoints)
            {
                Vector3 bridgePosition = new Vector3();
                Vector2 start = new Vector2(road.startPoint.x, road.startPoint.z);
                Vector2 end = new Vector2(road.endPoint.x, road.endPoint.z);

                bridgePosition.x = (terrainSize.x / heightMapResolution) * bridgePoint.x;
                bridgePosition.y = terrainSize.y * bridgePoint.y;
                bridgePosition.z = (terrainSize.x / heightMapResolution) * bridgePoint.z;

                float angle = CalculateAngle(start, end) * 180 / Mathf.PI;
                if (Mathf.Abs(angle) > 180)
                {
                    angle = angle % 180;
                }

                Instantiate(bridge, bridgePosition, Quaternion.Euler(0, 90 - angle, 0));
            }
        }
    }

    // TEXTURING BELOW THIS POINT!

    private void PlaceVegetationRandom(float[,] heightMap, System.Random rnd)
    {
        int maxHeight = heightData.GetLength(0) - 1;
        int maxWidth = heightData.GetLength(1) - 1;

        int vegetationAmount = 1000;

        int xCenter = rnd.Next(0 + vegetationRadius, maxWidth - vegetationRadius);
        int yCenter = rnd.Next(0 + vegetationRadius, maxHeight - vegetationRadius);

        int x, y, rotation, instantiator;
        for (int i = 0; i < vegetationAmount; i++)
        {
            x = rnd.Next(xCenter - vegetationRadius, xCenter + vegetationRadius);
            y = rnd.Next(yCenter - vegetationRadius, yCenter + vegetationRadius);

            Vector3 vegetationPosition = new Vector3();
            vegetationPosition.x = (terrainSize.x / heightMapResolution) * x;
            vegetationPosition.y = terrainSize.y * heightData[y, x];
            vegetationPosition.z = (terrainSize.x / heightMapResolution) * y;

            rotation = rnd.Next(0, 360);

            instantiator = rnd.Next(0, 100);


            if (instantiator <= 80)
            {
                Instantiate(vegetation[0], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
            }


            else if (instantiator <= 95)
            {
                Instantiate(vegetation[1], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
            }

            else if (instantiator < 98)
            {
                Instantiate(vegetation[2], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
            }

            else
            {
                Instantiate(vegetation[3], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
            }


        }
    }

    private void PlaceVegetation(float[,] heightMap, System.Random rnd)
    {
        int maxHeight = heightData.GetLength(0) - 1;
        int maxWidth = heightData.GetLength(1) - 1;

        int xCenter = rnd.Next(0 + vegetationRadius, maxWidth - vegetationRadius);
        int yCenter = rnd.Next(0 + vegetationRadius, maxHeight - vegetationRadius);

        int rotation, instantiator;
        for (int x = xCenter; x < xCenter + vegetationRadius; x+=3)
        {
            for(int y= yCenter; y < yCenter + vegetationRadius; y+=3)
            {
                Vector3 vegetationPosition = new Vector3();
                vegetationPosition.x = (terrainSize.x / heightMapResolution) * x;
                vegetationPosition.y = terrainSize.y * heightData[y, x];
                vegetationPosition.z = (terrainSize.x / heightMapResolution) * y;

                rotation = rnd.Next(0, 360);

                instantiator = rnd.Next(0, 100);

                if (instantiator <= 80)
                {
                    Instantiate(vegetation[0], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
                }


                else if (instantiator <= 95)
                {
                    Instantiate(vegetation[1], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
                }

                else if(instantiator < 98)
                {
                    Instantiate(vegetation[2], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
                }

                else
                {
                    Instantiate(vegetation[3], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
                }
            }

        }

        PlaceTrees(xCenter, yCenter, vegetationRadius);
    }

    private void PlaceVegetationWorld(float[,] heightMap, List<Lake> lakesList)
    {
        float heightTreshold = 0.016f;

        int maxHeight = heightData.GetLength(0) - 1;
        int maxWidth = heightData.GetLength(1) - 1;

        bool dontPlaceTexture = false;
        int rotation, instantiator;
        for(int x = 0; x < maxWidth; x++)
        {
            for(int y = 0; y < maxHeight; y++)
            {
                dontPlaceTexture = false;
                if(heightData[y, x] < heightTreshold)
                {
                    foreach (Lake lake in lakesList)
                    {
                        float ellipseEquationLakeEdge = Mathf.Pow(x - lake.midpoint.x, 2f) / Mathf.Pow(lake.lakeEdge.a, 2f) + Mathf.Pow(y - lake.midpoint.z, 2f) / Mathf.Pow(lake.lakeEdge.b, 2f);
                        if (ellipseEquationLakeEdge <= 1)
                        {
                            dontPlaceTexture = true;
                        }
                    }

                    if (!dontPlaceTexture)
                    {
                        foreach (River river in rivers)
                        {
                            for (int i = 0; i < river.riverPoints.Length; i++)
                            {
                                Vector2 riverPosition = new Vector2(river.riverPoints[i].x, river.riverPoints[i].z);
                                Vector2 currentPosition = new Vector2(x, y);
                                if(Vector2.Distance(riverPosition, currentPosition) <= river.width)
                                {
                                    dontPlaceTexture = true;
                                }
                            }
                        }
                    }

                    if (!dontPlaceTexture)
                    {
                        foreach(Road road in roads)
                        {
                            for (int i = 0; i < road.roadPoints.Length; i++)
                            {
                                Vector2 riverPosition = new Vector2(road.roadPoints[i].x, road.roadPoints[i].z);
                                Vector2 currentPosition = new Vector2(x, y);
                                if (Vector2.Distance(riverPosition, currentPosition) <= road.width + road.roadEdgeWidth/2)
                                {
                                    dontPlaceTexture = true;
                                }
                            }
                        }
                    }

                    if (!dontPlaceTexture)
                    {
                        Vector3 vegetationPosition = new Vector3();
                        vegetationPosition.x = (terrainSize.x / heightMapResolution) * x;
                        vegetationPosition.y = terrainSize.y * heightData[y, x];
                        vegetationPosition.z = (terrainSize.x / heightMapResolution) * y;

                        rotation = rnd.Next(0, 360);

                        instantiator = rnd.Next(0, 1000);

                        if(instantiator < 881)
                        {
                            Instantiate(vegetation[1], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
                        }
                        else if(instantiator < 887)
                        {
                            Instantiate(vegetation[2], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
                        }
                        else if (instantiator < 890)
                        {
                            Instantiate(vegetation[3], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
                        }
                        else if(instantiator == 890)
                        {
                            Instantiate(trees[0], vegetationPosition, Quaternion.Euler(0, 90 - rotation, 0));
                        }
                        
                    }
                }
                
            }
        }
    }

        private void PlaceTrees(int xCenter, int yCenter, int vegetationRadius)
    {

        int x, y, rotation, instantiator;
        for(int i = 0; i < numTrees; i++)
        {
            x = rnd.Next(xCenter, xCenter + vegetationRadius);
            y = rnd.Next(yCenter, yCenter + vegetationRadius);

            Vector3 fieldTreePosition = new Vector3();
            fieldTreePosition.x = (terrainSize.x / heightMapResolution) * x;
            fieldTreePosition.y = terrainSize.y * heightData[y, x];
            fieldTreePosition.z = (terrainSize.x / heightMapResolution) * y;

            rotation = rnd.Next(0, 360);

            instantiator = rnd.Next(0, 100);

            if (instantiator <= 50)
            {
                Instantiate(trees[0], fieldTreePosition, Quaternion.Euler(0, 90 - rotation, 0));
            }


            else if (instantiator <= 80)
            {
                Instantiate(trees[1], fieldTreePosition, Quaternion.Euler(0, 90 - rotation, 0));
            }

            else
            {
                Instantiate(trees[2], fieldTreePosition, Quaternion.Euler(0, 90 - rotation, 0));
            }
        }
    }

    private void PlaceWater(System.Random rnd)
    {
        foreach(Lake lake in lakes)
        {
            Vector3 waterPosition = new Vector3();
            waterPosition.x = (terrainSize.x / heightMapResolution) * lake.midpoint.x ;
            waterPosition.y = terrainSize.y * (lake.midpoint.y + lake.lakeEdgeHeightExtra/1.6f);
            waterPosition.z = (terrainSize.x / heightMapResolution) * lake.midpoint.z;

            float assetScalingFactorX = lake.a / 1.7f;
            float assetScalingFactorY = lake.b / 1.7f;
            Vector3 scalingVector = new Vector3(assetScalingFactorY, 0.01f, assetScalingFactorX);

            GameObject lakeWater = Instantiate(riverWater, waterPosition, Quaternion.Euler(0, 90, 0));
            lakeWater.transform.localScale = scalingVector;

            int instantiator;
            for(int i = 0; i < numWaterLillys; i++)
            {
                int offset = rnd.Next(0, 10);
                offset -= 3;
                Vector3 waterLillyPosition = new Vector3();

                waterLillyPosition.x = (terrainSize.x / heightMapResolution) * lake.midpoint.x + offset;

                offset = rnd.Next(0, 10);
                offset -= 5;

                waterLillyPosition.y = terrainSize.y * (lake.midpoint.y + lake.lakeEdgeHeightExtra / 1.6f);
                waterLillyPosition.z = (terrainSize.x / heightMapResolution) * lake.midpoint.z + offset;

                instantiator = rnd.Next(0, 100);
                if(instantiator < 50)
                {
                    Instantiate(waterLilly[0], waterLillyPosition, Quaternion.Euler(0, instantiator, 0));
                }
                else
                {
                    Instantiate(waterLilly[1], waterLillyPosition, Quaternion.Euler(0, 90, 0));
                }
            }

        }

        foreach(River river in rivers)
        {
            for (int i = 0; i < river.riverPoints.Length; i++)
            {
                Vector3 riverWaterPosition = new Vector3();
                riverWaterPosition.x = (terrainSize.x / heightMapResolution) * river.riverPoints[i].x;
                riverWaterPosition.y = terrainSize.y * (river.riverPoints[i].y + river.riverDepth/ 1.6f);
                riverWaterPosition.z = (terrainSize.x / heightMapResolution) * river.riverPoints[i].z;
                Instantiate(riverWater, riverWaterPosition, Quaternion.Euler(0, 90, 0));
            }
        }
      
    }

    private void PlaceRoad()
    {
        foreach(Road path in roads)
        {
            for(int i = 0; i < path.roadPoints.Length; i++)
            {
                if(i % 10 == 0)
                {
                    Vector2 start = new Vector2(path.startPoint.x, path.startPoint.z);
                    Vector2 end = new Vector2(path.endPoint.x, path.endPoint.z);

                    Vector3 roadPosition = new Vector3(); 
                    roadPosition.x = (terrainSize.x / heightMapResolution) * path.roadPoints[i].x;
                    roadPosition.y = terrainSize.y * (path.roadPoints[i].y + 0.001f);

                    if(start.x < end.x)
                    {
                        roadPosition.z = (terrainSize.x / heightMapResolution) * (path.roadPoints[i].z - 9);
                    }
                    else
                    {
                        roadPosition.z = (terrainSize.x / heightMapResolution) * (path.roadPoints[i].z + 9);
                    }
                   


                    float angle = CalculateAngle(start, end) * 180 / Mathf.PI;
                    if (Mathf.Abs(angle) > 180)
                    {
                        angle = angle % 180;
                    }

                    Instantiate(road, roadPosition, Quaternion.Euler(0, 90 - angle, 0));
                }
            }
        }
    }

    public float CalculateAngle(Vector2 start, Vector2 end)
    {
        float adjacent = end.x - start.x;
        float opposite = end.y - start.y;
        float angle = Mathf.Atan2(opposite, adjacent);


        return angle;
    }

}