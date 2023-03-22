using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class MapGenerator : MonoBehaviour {
    public int width;
    public int height;
    [Range(0,100)]
    public int randomFillPercent;
    public string seed;
    public bool useRandomSeed;
    List<Room> survivingRooms;

    int[,] map;

    void Start() {
        GenerateMap();
    }

    void Update() {
        if(Input.GetMouseButtonDown(1)){
            GenerateMap();
        }
    }

    void GenerateMap() {
        map = new int[width, height];
        RandomFillMap();

        for(int i = 0; i< 5; i++) {
            SmoothMap();
        }

        ProcessMap();

        int borderSize = 2;
        int[,] borderedMap = new int[width + borderSize * 2, height + borderSize * 2];
    
        for(int x = 0; x < borderedMap.GetLength(0); x++) {
            for(int y = 0; y < borderedMap.GetLength(1); y++) {
                if(x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize) {
                    borderedMap[x,y] = map[x - borderSize, y - borderSize];
                } else {
                    borderedMap[x,y] = 1;
                }
            }
        }

        MeshGenerator meshGen = GetComponent<MeshGenerator>();
        meshGen.GenerateMesh(borderedMap, 5);
    }
    //generate room meshes by passing co-ordinates of all rooms
    void ProcessMap() {
        List<List<Coordinate>> wallRegions = GetRegions(1);
        int wallThresholdSize = 50;

        foreach(List<Coordinate> wallRegion in wallRegions) {
            if(wallRegion.Count < wallThresholdSize) {
                foreach(Coordinate tile in wallRegion) {
                    map[tile.tileX, tile.tileY] = 0;
                }
            }
        }

        List<List<Coordinate>> roomRegions = GetRegions(0);
        int roomThresholdSize = 50;
        survivingRooms = new List<Room>();

        foreach(List<Coordinate> roomRegion in roomRegions) {
            if(roomRegion.Count < roomThresholdSize) {
                foreach(Coordinate tile in roomRegion) {
                    map[tile.tileX, tile.tileY] = 1;
                }
            }
            else {
                survivingRooms.Add(new Room(roomRegion, map));
            }
        }
        survivingRooms.Sort();
        survivingRooms[0].isMainRoom = true;
        survivingRooms[0].isAccessibleFromMainRoom = true;
        //ConnectClosestRooms(survivingRooms);
    }

    void ConnectClosestRooms(List<Room> allRooms, bool forceAccessibilityFromMainRoom = false) {

        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if(forceAccessibilityFromMainRoom) {
            foreach(Room room in allRooms) {
                if(room.isAccessibleFromMainRoom) {
                    roomListB.Add(room);
                } else {
                    roomListA.Add(room);
                }
            }
        } else {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Coordinate bestTileA = new Coordinate();
        Coordinate bestTileB = new Coordinate();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach(Room roomA in roomListA) {
            if(!forceAccessibilityFromMainRoom) {
                possibleConnectionFound = false;

                if(roomA.connectedRooms.Count > 0) {
                    continue;
                }
            }

            foreach (Room roomB in roomListB) {
                if(roomA == roomB || roomA.IsConnected(roomB)) {
                    continue;
                }

                for(int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++) {
                    for(int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++) {
                        Coordinate tileA = roomA.edgeTiles[tileIndexA];
                        Coordinate tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + MathF.Pow(tileA.tileY - tileB.tileY, 2));

                        if(distanceBetweenRooms < bestDistance || !possibleConnectionFound) {
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

            if(possibleConnectionFound && !forceAccessibilityFromMainRoom) {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }

        if(possibleConnectionFound && forceAccessibilityFromMainRoom) {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(allRooms, true);
        }

        if(!forceAccessibilityFromMainRoom) {
            ConnectClosestRooms(allRooms, true);
        }
    }

    void CreatePassage(Room roomA, Room roomB, Coordinate tileA, Coordinate tileB) {
        Room.ConnectRooms(roomA, roomB);
        Debug.DrawLine(CoordinateToWorldPoint(tileA), CoordinateToWorldPoint(tileB), Color.cyan, 100);

        List<Coordinate> line = GetLine(tileA, tileB);
        foreach (Coordinate c in line) {
            DrawCircle(c, 2);
        } 
    }

    void DrawCircle(Coordinate c, int r) {
        for (int x = -r; x <= r; x++) {
            for (int y = -r; y <= r; y++) { 
                if (x*x + y*y <= r*r) {
                    int drawX = c.tileX + x;
                    int drawY = c.tileY + y;
                    if(IsInMapRange(drawX, drawY)) {
                        map[drawX, drawY] = 0;
                    }
                }
            }    
        }
    }

    List<Coordinate> GetLine(Coordinate from, Coordinate to) {
        List<Coordinate> line = new List<Coordinate>();

        int x = from.tileX;
        int y = from.tileY;

        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if(longest < shortest) {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for(int i = 0; i < longest; i++) {
            line.Add(new Coordinate(x,y));

            if(inverted) {
                y += step;
            } else {
                x += step;
            }

            gradientAccumulation += shortest;
            if(gradientAccumulation >= longest) {
                if(inverted) {
                    x += gradientStep;
                } else {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }
        return line;
    }

    Vector3 CoordinateToWorldPoint(Coordinate tile) {
        return new Vector3(-width/2 + .5f + tile.tileX, 2, -height/2 + .5f + tile.tileY);
    }

    List<List<Coordinate>> GetRegions(int tileType) {
        List<List<Coordinate>> regions = new List<List<Coordinate>>();
        int[,] mapFlags = new int[width,height];
        
        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                if(mapFlags[x,y] == 0 && map[x,y] == tileType) {
                    List<Coordinate> newRegion = GetRegionTiles(x,y);
                    regions.Add(newRegion);

                    foreach(Coordinate tile in newRegion) {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }
        return regions;
    }

    List<Coordinate> GetRegionTiles(int startX, int startY) {
        List<Coordinate> tiles = new List<Coordinate>();
        int[,] mapFlags = new int[width,height];
        int tileType = map[startX, startY];

        Queue<Coordinate> queue = new Queue<Coordinate>();
        queue.Enqueue(new Coordinate(startX, startY));
        mapFlags[startX,startY] = 1;

        while(queue.Count > 0) {
            Coordinate tile = queue.Dequeue();
            tiles.Add(tile);

            for(int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
                for(int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
                    if(IsInMapRange(x, y) && (y == tile.tileY || x == tile.tileX)) {
                        if(mapFlags[x,y] == 0 && map[x,y] == tileType) {
                            mapFlags[x,y] = 1;
                            queue.Enqueue(new Coordinate(x,y));
                        }
                    }
                }
            }
        }
        return tiles;
    }

    bool IsInMapRange(int x, int y) {
        return x >= 0 && x < width && y >=0 && y < height;
    }


    void RandomFillMap() {
        if(useRandomSeed) {
            seed = Time.time.ToString();
        }

        System.Random randNumGenerator = new System.Random(seed.GetHashCode());

        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                if(x == 0 || x == width -1 || y == 0 || y == height -1) {
                    map[x,y] = 1;
                } else {
                    map[x,y] = (randNumGenerator.Next(0,100) < randomFillPercent)? 1 : 0;
                }
            }
        }
    }

    void SmoothMap() {
        for(int x = 0; x < width; x++) {
            for(int y = 0; y < height; y++) {
                int neighbourWallTiles = GetSurroundingWallCount(x,y);

                if(neighbourWallTiles > 4) {
                    map[x,y] = 1;
                } else if (neighbourWallTiles < 4) {
                    map[x,y] = 0;
                }
            }   
        }
    }

    int GetSurroundingWallCount(int gridX, int gridY) {
        int wallCount = 0;
        for(int neighbourX = gridX -1; neighbourX <= gridX + 1; neighbourX++) {
            for(int neighbourY = gridY -1; neighbourY <= gridY + 1; neighbourY++) {  
                if(IsInMapRange(neighbourX, neighbourY)) {
                    if(neighbourX != gridX || neighbourY != gridY){
                        wallCount += map[neighbourX, neighbourY];
                    }
                } else {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    struct Coordinate {
        public int tileX, tileY;

        public Coordinate(int x, int y) {
            tileX = x;
            tileY = y;
        }
    }

    class Room : IComparable<Room> {
        public List<Coordinate> tiles;
        public List<Coordinate> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;
        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;

        public Room() {}

        public Room(List<Coordinate> roomTiles, int[,] map) {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();
            edgeTiles = new List<Coordinate>();
            foreach(Coordinate tile in tiles) {
                for(int x = tile.tileX - 1; x <= tile.tileX + 1; x++) {
                    for(int y = tile.tileY - 1; y <= tile.tileY + 1; y++) {
                        if(x == tile.tileX || y == tile.tileY) {
                            if(map[x,y] == 1) {
                                edgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }
        }

        public int CompareTo(Room otherRoom) {
            return otherRoom.roomSize.CompareTo(roomSize);
        }

        public static void ConnectRooms(Room roomA, Room roomB) {
            if(roomA.isAccessibleFromMainRoom) {
                roomB.SetAccessibleibleFromMainRoom();
            } else if (roomB.isAccessibleFromMainRoom) {
                roomA.SetAccessibleibleFromMainRoom();
            }
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom) {
            return connectedRooms.Contains(otherRoom);
        }

        public void SetAccessibleibleFromMainRoom() {
            if(!isAccessibleFromMainRoom){
                isAccessibleFromMainRoom = true;
                foreach(Room connectedRoom in connectedRooms) {
                    connectedRoom.SetAccessibleibleFromMainRoom();
                }
            }
        }
    }
}
