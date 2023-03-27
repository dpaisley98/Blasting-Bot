using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;

public class MapGenerator : MonoBehaviour 
{
    public int width, height;
    [Range(0,100)]
    public int randomFillPercent;
    public int groundTile = 1, platformTile = 0;
    public string seed;
    public bool useRandomSeed;
    List<Room> survivingRooms;
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

        foreach(Room room in survivingRooms) 
        {
            for(int tileIndex = 0; tileIndex < room.tiles.Count; tileIndex++) 
            {
                heightMap[room.tiles[tileIndex].tileX, room.tiles[tileIndex].tileY] = room.roomHeight;
            }

            for(int tileIndex = 0; tileIndex < room.edgeTiles.Count; tileIndex++) 
            {
                heightMap[room.edgeTiles[tileIndex].tileX, room.edgeTiles[tileIndex].tileY] = room.roomHeight;
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
        List<List<Coordinate>> wallRegions = GetRegions(groundTile);
        int wallThresholdSize = 40;

        foreach(List<Coordinate> wallRegion in wallRegions) 
        {
            if(wallRegion.Count < wallThresholdSize) 
            {
                foreach(Coordinate tile in wallRegion) 
                {
                    map[tile.tileX, tile.tileY] = platformTile;
                }
            }
        }

        List<List<Coordinate>> roomRegions = GetRegions(platformTile);
        int roomThresholdSize = 35;
        survivingRooms = new List<Room>();
        platformMaps = new List<int[,]>();

        foreach(List<Coordinate> roomRegion in roomRegions) 
        {
            if(roomRegion.Count < roomThresholdSize) 
            {
                foreach(Coordinate tile in roomRegion) 
                {
                    map[tile.tileX, tile.tileY] = groundTile;
                }
            }
            else 
            {
                Room survivingRoom = new Room(roomRegion, map);
                survivingRooms.Add(survivingRoom);
            }
        }
        survivingRooms.Sort();
        survivingRooms[0].isMainRoom = true;
        survivingRooms[0].isAccessibleFromMainRoom = true;
    }

    void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false) 
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if(forceAccessibilityFromMainRoom) 
        {
            foreach(Room room in allRooms) 
            {
                if(room.isAccessibleFromMainRoom) 
                {
                    roomListB.Add(room);
                } else 
                {
                    roomListA.Add(room);
                }
            }
        } else 
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Coordinate bestTileA = new Coordinate();
        Coordinate bestTileB = new Coordinate();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach(Room roomA in roomListA) 
        {
            if(!forceAccessibilityFromMainRoom) 
            {
                possibleConnectionFound = false;

                if(roomA.connectedRooms.Count > 0) 
                {
                    continue;
                }
            }

            foreach (Room roomB in roomListB) 
            {
                if(roomA == roomB || roomA.IsConnected(roomB)) 
                {
                    continue;
                }

                for(int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++) 
                {
                    for(int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++) 
                    {
                        Coordinate tileA = roomA.edgeTiles[tileIndexA];
                        Coordinate tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + MathF.Pow(tileA.tileY - tileB.tileY, 2));

                        if(distanceBetweenRooms < bestDistance || !possibleConnectionFound) 
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }
        }

        if(!forceAccessibilityFromMainRoom) 
        {
            ConnectClosestRooms(allRooms, true);
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
        if(useRandomSeed) 
        {
            seed = Time.time.ToString();
        }

        System.Random randNumGenerator = new System.Random(seed.GetHashCode());

        for(int x = 0; x < width; x++) 
        {
            for(int y = 0; y < height; y++) {
                if(x == 0 || x == width -1 || y == 0 || y == height -1) 
                {
                    map[x,y] = groundTile;
                } else 
                {
                    map[x,y] = (randNumGenerator.Next(0,100) < randomFillPercent)? groundTile : platformTile;
                }
            }
        }
    }

    void SmoothMap() 
    {
        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) 
            {
                int neighbourgroundTiles = GetSurroundingWallCount(x,y);

                if(neighbourgroundTiles > 4) 
                {
                    map[x,y] = groundTile;
                } else if (neighbourgroundTiles < 4) 
                {
                    map[x,y] = platformTile;
                }
            }   
        }
    }

    int GetSurroundingWallCount(int gridX, int gridY) 
    {
        int wallCount = 0;
        for(int neighbourX = gridX -1; neighbourX <= gridX + 1; neighbourX++) 
        {
            for(int neighbourY = gridY -1; neighbourY <= gridY + 1; neighbourY++) 
            {  
                if(IsInMapRange(neighbourX, neighbourY)) 
                {
                    if(neighbourX != gridX || neighbourY != gridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                } else 
                {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    public int GetHeightValueFromHeightMap (int x, int y)
    {
        Debug.Log("X : " + x + (-width/2) + " Y: " + y + (-height/2));
        return borderedHeightMap[x + (-width/2), y + (-height/2)];
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

    class Room : IComparable<Room> 
    {
        public List<Coordinate> tiles;
        public List<Coordinate> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;
        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;
        public int roomHeight;

        public Room() {}

        public Room(List<Coordinate> platformTiles, int[,] map) 
        {
            tiles = platformTiles;
            roomSize = tiles.Count;
            roomHeight = UnityEngine.Random.Range(20, 95);
            connectedRooms = new List<Room>();
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
                            {
                                edgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }
        }

        public int CompareTo(Room otherRoom) 
        {
            return otherRoom.roomSize.CompareTo(roomSize);
        }

        public bool IsConnected(Room otherRoom) 
        {
            return connectedRooms.Contains(otherRoom);
        }

        public void SetAccessibleibleFromMainRoom() 
        {
            if(!isAccessibleFromMainRoom)
            {
                isAccessibleFromMainRoom = true;
                foreach(Room connectedRoom in connectedRooms) 
                {
                    connectedRoom.SetAccessibleibleFromMainRoom();
                }
            }
        }
    }
}
