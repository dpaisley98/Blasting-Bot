using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;

public class MapGenerator : MonoBehaviour 
{
    public int width, height;
    [Range(0f,1f)]
    public float randomFillPercent;
    public int groundTile = 1, platformTile = 0;
    List<Platform> survivingPlatforms;
    List<int[,]> platformMaps;
    int[,] map, borderedHeightMap;

    void Start() 
    {
        GenerateMap();
    }

    void Update() 
    {
        if(Input.GetMouseButtonDown(1))
        {
            GenerateMap();
        }
    }

    public void GenerateMap() 
    {
        MapSpawner mapSpawner = GetComponent<MapSpawner>();
        mapSpawner.StopSpawningEnemies();
        map = new int[width, height];
        RandomFillMap();

        for(int i = 0; i < 5; i++) 
        {
            SmoothMap();
        }

        ProcessMap();

        int borderSize = 2;
        int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

        int[,] heightMap = new int[width, height];
        borderedHeightMap = new int[width + borderSize * 2, height + borderSize * 2];

        foreach(Platform platform in survivingPlatforms) 
        {
            for(int tileIndex = 0; tileIndex < platform.tiles.Count; tileIndex++) 
            {
                heightMap[platform.tiles[tileIndex].tileX, platform.tiles[tileIndex].tileY] = platform.platformHeight;
            }

            for(int tileIndex = 0; tileIndex < platform.edgeTiles.Count; tileIndex++) 
            {
                heightMap[platform.edgeTiles[tileIndex].tileX, platform.edgeTiles[tileIndex].tileY] = platform.platformHeight;
            }
        }

        for(int x = 1; x < heightMap.GetLength(0) -1; x++) 
        {
            for(int y = 1; y < heightMap.GetLength(1) -1; y++) 
            {
                if(CheckNeighbourTileType(new Coordinate(x, y), new Coordinate(x + 1, y))) 
                    heightMap[x, y] = heightMap[x + 1, y];

                else if(CheckNeighbourTileType(new Coordinate(x, y), new Coordinate(x, y + 1))) 
                    heightMap[x, y] = heightMap[x, y + 1];

                else if(CheckNeighbourTileType(new Coordinate(x, y), new Coordinate(x + 1, y + 1)))
                    heightMap[x, y] = heightMap[x + 1, y + 1];

                else if(CheckNeighbourTileType(new Coordinate(x, y), new Coordinate(x, y - 1))) 
                    heightMap[x, y] = heightMap[x, y - 1];
            }
        }

        for(int x = 0; x < borderedMap.GetLength(0); x++) 
        {
            for(int y = 0; y < borderedMap.GetLength(1); y++) 
            {
                if(x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize) 
                {
                    borderedMap[x,y] = map[x - borderSize, y - borderSize];
                    borderedHeightMap[x,y] = heightMap[x - borderSize, y - borderSize];
                } else 
                {
                    borderedMap[x,y] = groundTile;
                    borderedHeightMap[x,y] = 0;
                }
            }
        }

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderedMap, 5, borderedHeightMap);
        mapSpawner.StartSpawningEnemies();
        mapSpawner.SpawnPlayer();
        mapSpawner.SpawnTeleporter();
    }

    void ProcessMap() 
    {
        List<List<Coordinate>> groundRegions = GetRegions(groundTile);
        int groundThresholdSize = 40;

        foreach(List<Coordinate> groundRegion in groundRegions) 
        {
            if(groundRegion.Count < groundThresholdSize) 
            {
                foreach(Coordinate tile in groundRegion) 
                {
                    map[tile.tileX, tile.tileY] = platformTile;
                }
            }
        }

        List<List<Coordinate>> platformRegions = GetRegions(platformTile);
        int platformThresholdSize = 35;
        survivingPlatforms = new List<Platform>();
        platformMaps = new List<int[,]>();

        foreach(List<Coordinate> platformRegion in platformRegions) 
        {
            if(platformRegion.Count < platformThresholdSize) 
            {
                foreach(Coordinate tile in platformRegion) 
                {
                    map[tile.tileX, tile.tileY] = groundTile;
                }
            }
            else 
            {
                Platform survivingPlatform = new Platform(platformRegion, map);
                survivingPlatforms.Add(survivingPlatform);
            }
        }
    }

    List<List<Coordinate>> GetRegions(int tileType) 
    {
        List<List<Coordinate>> regions = new List<List<Coordinate>>();
        int[,] mapFlags = new int[width,height];
        
        for(int x = 0; x < width; x++) 
        {
            for(int y = 0; y < height; y++) 
            {
                if(mapFlags[x,y] == 0 && map[x,y] == tileType) 
                {
                    List<Coordinate> newRegion = GetRegionTiles(x,y);
                    regions.Add(newRegion);

                    foreach(Coordinate tile in newRegion) 
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }
        return regions;
    }

    bool CheckNeighbourTileType(Coordinate currentTile, Coordinate neighbourTile)
    {
        return map[currentTile.tileX, currentTile.tileY] != platformTile && map[neighbourTile.tileX, neighbourTile.tileY] == platformTile;
    }

    List<Coordinate> GetRegionTiles(int startX, int startY) 
    {
        List<Coordinate> tiles = new List<Coordinate>();
        int[,] mapFlags = new int[width,height];
        int tileType = map[startX, startY];

        Queue<Coordinate> queue = new Queue<Coordinate>();
        queue.Enqueue(new Coordinate(startX, startY));
        mapFlags[startX,startY] = 1;

        while(queue.Count > 0) 
        {
            Coordinate tile = queue.Dequeue();
            tiles.Add(tile);

            for(int x = tile.tileX - 1; x <= tile.tileX + 1; x++) 
            {
                for(int y = tile.tileY - 1; y <= tile.tileY + 1; y++) 
                {
                    if(IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX)) 
                    {
                        if(mapFlags[x,y] == 0 && map[x,y] == tileType) 
                        {
                            mapFlags[x,y] = 1;
                            queue.Enqueue(new Coordinate(x,y));
                        }
                    }
                }
            }
        }
        return tiles;
    }

    bool IsInMapRange(int x, int y) 
    {
        return x >= 0 && x < width && y >=0 && y < height;
    }

    void RandomFillMap() 
    {
        float xOffset = UnityEngine.Random.Range(-10000f, 10000);
        float yOffset = UnityEngine.Random.Range(-10000f, 10000);

        for(int x = 0; x < width; x++) 
        {
            for(int y = 0; y < height; y++) {
                if(x == 0 || x == width -1 || y == 0 || y == height -1) 
                    map[x,y] = groundTile;
                else 
                    map[x,y] = ((Mathf.PerlinNoise(x * xOffset, y * yOffset)) < randomFillPercent)? groundTile : platformTile;
            }
        }
    }

    void SmoothMap() 
    {
        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) 
            {
                int neighbourgroundTiles = GetSurroundingGroundTiles(x,y);

                if(neighbourgroundTiles > 4) 
                    map[x,y] = groundTile;
                else if (neighbourgroundTiles < 4) 
                    map[x,y] = platformTile;
            }   
        }
    }

    int GetSurroundingGroundTiles(int gridX, int gridY) 
    {
        int groundCount = 0;
        for(int neighbourX = gridX -1; neighbourX <= gridX + 1; neighbourX++) 
        {
            for(int neighbourY = gridY -1; neighbourY <= gridY + 1; neighbourY++) 
            {  
                if(IsInMapRange(neighbourX, neighbourY)) 
                {
                    if(neighbourX != gridX || neighbourY != gridY)
                        groundCount += map[neighbourX, neighbourY];
                } else 
                {
                    groundCount++;
                }
            }
        }
        return groundCount;
    }

    struct Coordinate 
    {
        public int tileX, tileY;

        public Coordinate(int x, int y) 
        {
            tileX = x;
            tileY = y;
        }
    }

    class Platform
    {
        public List<Coordinate> tiles;
        public List<Coordinate> edgeTiles;
        public int platformSize;
        public int platformHeight;

        public Platform() {}

        public Platform(List<Coordinate> platformTiles, int[,] map) 
        {
            tiles = platformTiles;
            platformSize = tiles.Count;
            platformHeight = UnityEngine.Random.Range(20, 95);
            edgeTiles = new List<Coordinate>();
            foreach(Coordinate tile in tiles) 
            {
                for(int x = tile.tileX - 1; x <= tile.tileX + 1; x++) 
                {
                    for(int y = tile.tileY - 1; y <= tile.tileY + 1; y++) 
                    {
                        if(x == tile.tileX || y == tile.tileY) 
                        {
                            if(map[x,y] == 1) 
                                edgeTiles.Add(tile);
                        }
                    }
                }
            }
        }
    }
}
