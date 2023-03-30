using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class MeshGenerator : MonoBehaviour {
    List<Vector3> vertices;
    List<int> triangles;
    Dictionary<int, List<Triangle>> triangleDict = new Dictionary<int, List<Triangle>>();
    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();
    public MeshFilter walls, ground, platforms;

    public void GenerateMesh(int[,] map, float squareSize, int[,] heightMap)
    {
        IsMeshGenerated = false;
        triangleDict.Clear();
        outlines.Clear();
        checkedVertices.Clear();
        SquareGrid squareGrid = new SquareGrid(map, squareSize);
        vertices = new List<Vector3>();
        triangles = new List<int>();

        for(int x = 0; x < squareGrid.squares.GetLength(0); x++ ) 
        {
            for(int y = 0; y < squareGrid.squares.GetLength(1); y++ ) 
            { 
                TriangulateSquares(squareGrid.squares[x,y]);
            }
        }

        Mesh mesh = new Mesh();
        ground.mesh = mesh;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        int tileAmount = 10;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++) 
        {
            float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].x) * tileAmount;
            float percentY = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].z) * tileAmount;
            uvs[i] = new Vector2(percentX, percentY);
        }

        mesh.uv = uvs;

        MeshCollider currentCollider = ground.gameObject.GetComponent<MeshCollider>();
        Destroy(currentCollider);
        MeshCollider levelCollider = ground.gameObject.AddComponent<MeshCollider>();
        levelCollider.sharedMesh = mesh;

        CreatePlatformMesh(map, squareSize, heightMap);
        CreateWallMesh();
        GenerateNavMesh();
        IsMeshGenerated = true;
    }

    void GenerateNavMesh() 
    {
        NavMesh.RemoveAllNavMeshData();

        NavMeshData navMeshData = NavMeshBuilder.BuildNavMeshData(
            NavMesh.GetSettingsByID(0), // NavMeshBuildSettings
            new List<NavMeshBuildSource> 
            {
                new NavMeshBuildSource 
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = ground.mesh,
                    transform = ground.transform.localToWorldMatrix,
                    area = 0
                },
                new NavMeshBuildSource 
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = walls.mesh,
                    transform = walls.transform.localToWorldMatrix,
                    area = 1
                },
                new NavMeshBuildSource 
                {
                    shape = NavMeshBuildSourceShape.Mesh,
                    sourceObject = platforms.mesh,
                    transform = platforms.transform.localToWorldMatrix,
                    area = 0
                },
                new NavMeshBuildSource 
                {
                    shape = NavMeshBuildSourceShape.Box,
                    size = new Vector3(1000, 250, 1000),
                    transform = transform.localToWorldMatrix,
                    area = 10 // The same layer that you set for your NavMeshSurface
                }
            }, 
            new Bounds(Vector3.zero, new Vector3(1000, 1000, 1000)), 
            Vector3.zero, 
            Quaternion.identity);

        NavMesh.AddNavMeshData(navMeshData);

        // Assign NavMesh to NavMeshSurface if one exists
        NavMeshSurface navMeshSurface = GetComponent<NavMeshSurface>();
        if (navMeshSurface != null) 
        {
            navMeshSurface.RemoveData();
            navMeshSurface.BuildNavMesh();
        }
    }

    void CreatePlatformMesh(int[,] map, float squareSize, int[,] heightMap) 
    {
        triangleDict.Clear();
        outlines.Clear();
        checkedVertices.Clear();
        SquareGrid squareGrid = new SquareGrid(map, squareSize, heightMap);
        vertices = new List<Vector3>();
        triangles = new List<int>();

        for(int x = 0; x < squareGrid.squares.GetLength(0); x++ ) 
        {
            for(int y = 0; y < squareGrid.squares.GetLength(1); y++ ) 
            { 
                TriangulateSquares(squareGrid.squares[x,y]);
            }
        }

        Mesh platformMesh = new Mesh();
        platforms.mesh = platformMesh;
        platformMesh.vertices = vertices.ToArray();
        platformMesh.triangles = triangles.ToArray();
        platformMesh.RecalculateNormals();

        MeshCollider currentCollider = platforms.gameObject.GetComponent<MeshCollider> ();
        Destroy(currentCollider);
        MeshCollider wallCollider = platforms.gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = platformMesh;
    }

    void CreateWallMesh() 
    {
        CalculateMeshOutlines();
        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float wallHeight;

        foreach(List<int> outline in outlines) 
        {
            for (int i =0; i < outline.Count -1; i++) 
            {
                wallHeight = vertices[outline[i]].y;

                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]);
                wallVertices.Add(vertices[outline[i+1]]);
                wallVertices.Add(vertices[outline[i]] - Vector3.up * wallHeight);
                wallVertices.Add(vertices[outline[i+1]] - Vector3.up * wallHeight);

                wallTriangles.Add(startIndex);
                wallTriangles.Add(startIndex + 2);
                wallTriangles.Add(startIndex + 3);

                wallTriangles.Add(startIndex + 3);
                wallTriangles.Add(startIndex + 1);
                wallTriangles.Add(startIndex);
            }
        }

        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        walls.mesh = wallMesh;

        MeshCollider currentCollider = walls.gameObject.GetComponent<MeshCollider> ();
        Destroy(currentCollider);
        MeshCollider wallCollider = walls.gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;
    }

    void TriangulateSquares(Square square) 
    {
        switch(square.configuration) 
        {
            case 0:
                break;
            case 1:
                MeshFromPoints(square.centreLeft, square.centreBottom, square.bottomLeft);
                break;
            case 2:
                MeshFromPoints(square.bottomRight, square.centreBottom, square.centreRight);
                break;
            case 4:
                MeshFromPoints(square.topRight, square.centreRight, square.centreTop);
                break;
            case 8:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreLeft);
                break;
            case 3:
                MeshFromPoints(square.centreRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 6:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.centreBottom);
                break;
            case 9:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreBottom, square.bottomLeft);
                break;
            case 12:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreLeft);
                break;
            case 5:
                MeshFromPoints(square.centreTop, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft, square.centreLeft);
                break;
            case 10:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;
            case 7:
                MeshFromPoints(square.centreTop, square.topRight, square.bottomRight, square.bottomLeft, square.centreLeft);
                break;
            case 11:
                MeshFromPoints(square.topLeft, square.centreTop, square.centreRight, square.bottomRight, square.bottomLeft);
                break;
            case 13:
                MeshFromPoints(square.topLeft, square.topRight, square.centreRight, square.centreBottom, square.bottomLeft);
                break;
            case 14:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.centreBottom, square.centreLeft);
                break;
            case 15:
                MeshFromPoints(square.topLeft, square.topRight, square.bottomRight, square.bottomLeft);
                checkedVertices.Add(square.topLeft.vertexIndex);
                checkedVertices.Add(square.topRight.vertexIndex);
                checkedVertices.Add(square.bottomLeft.vertexIndex);
                checkedVertices.Add(square.bottomRight.vertexIndex);
                break;
        }
    }

    void MeshFromPoints(params Node[] points) 
    {
        AssignVertices(points);
        if(points.Length >= 3) 
            CreateTriangle(points[0], points[1], points[2]);

        if(points.Length >= 4) 
            CreateTriangle(points[0], points[2], points[3]);

        if(points.Length >= 5) 
            CreateTriangle(points[0], points[3], points[4]);

        if(points.Length >= 6) 
            CreateTriangle(points[0], points[4], points[5]);
    }

    void AssignVertices(Node[] points) 
    {
        for(int i = 0; i < points.Length; i++) 
        {
            if(points[i].vertexIndex == -1) 
            {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    struct Triangle 
    {
        public int vertexIndexA;                
        public int vertexIndexB;
        public int vertexIndexC;

        int[] vertices;

        public Triangle (int a, int b, int c) 
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public int this[int i] 
        {
            get 
            {
                return vertices[i];
            }
        }

        public bool Contains(int vertexIndex) 
        {
            return vertexIndexA == vertexIndex || vertexIndexB == vertexIndex || vertexIndexC == vertexIndex;
        }
    }

    void CreateTriangle(Node a, Node b, Node c) 
    {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle (a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictionary(triangle.vertexIndexA, triangle);
        AddTriangleToDictionary(triangle.vertexIndexB, triangle);
        AddTriangleToDictionary(triangle.vertexIndexC, triangle);
    }

    void AddTriangleToDictionary(int vertextIndexKey, Triangle triangle) 
    {
        if(triangleDict.ContainsKey(vertextIndexKey)) 
        {
            triangleDict[vertextIndexKey].Add(triangle);
        } else 
        {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDict.Add(vertextIndexKey, triangleList);
        }
    }

    void CalculateMeshOutlines() 
    {
        for(int vertexI = 0; vertexI < vertices.Count; vertexI++) 
        {
            if(!checkedVertices.Contains(vertexI))
            {
                int newOutlineVertex = GetConnectedOutlineVertex(vertexI);
                if(newOutlineVertex != -1) {
                    checkedVertices.Add(vertexI);

                    List<int> newOutline = new List<int>();
                    newOutline.Add(vertexI);
                    outlines.Add(newOutline);
                    FollowOutline(newOutlineVertex, outlines.Count -1);
                    outlines[outlines.Count -1].Add(vertexI);
                }
            }
        }
    }

    void FollowOutline(int vertexIndex, int outlineIndex) 
    {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if(nextVertexIndex != -1) 
        {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }
    
    int GetConnectedOutlineVertex(int vertexIndex) 
    {
        List<Triangle> trianglesContainigVertex = triangleDict[vertexIndex];

        for(int i = 0; i < trianglesContainigVertex.Count; i++) 
        { 
            Triangle triangle = trianglesContainigVertex[i];

            for(int j = 0; j < 3; j++) 
            {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                {
                    if(IsOutlineEdge(vertexIndex, vertexB)) 
                    {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    bool IsOutlineEdge(int vertexA, int vertexB) 
    {
        List<Triangle> trianglesContainigVertexA = triangleDict[vertexA];
        int sharedTriangleCount = 0;

        for(int i = 0; i < trianglesContainigVertexA.Count; i++) 
        {
            if(trianglesContainigVertexA[i].Contains(vertexB)) 
            {
                sharedTriangleCount++;
                if(sharedTriangleCount > 1)
                {
                    break;
                }
            }
        }

        return sharedTriangleCount == 1;
    }

    public bool IsMeshGenerated { get; private set; } = false;

    public class SquareGrid 
    {
        public Square[,] squares, squarePlatforms;

        public SquareGrid(int[,] map, float squareSize) 
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodesGround = new ControlNode[nodeCountX, nodeCountY];

            for(int x = 0; x < nodeCountX; x++ ) 
            {
                for(int y = 0; y < nodeCountY; y++ ) 
                {
                    Vector3 position = new Vector3(-mapWidth/2 + x * squareSize + squareSize/2, 0, -mapHeight/2 + y * squareSize + squareSize/2);
                    controlNodesGround[x,y] = new ControlNode(position, map[x,y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX -1, nodeCountY -1];
            for(int x = 0; x < nodeCountX -1; x++ ) 
            {
                for(int y = 0; y < nodeCountY -1; y++ ) 
                {
                    squares[x,y] = new Square(controlNodesGround[x,y+1], controlNodesGround[x+1,y+1], controlNodesGround[x+1,y], controlNodesGround[x,y]);
                }
            }           
        }

        public SquareGrid(int[,] map, float squareSize, int[,] height) 
        {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodesPlatforms = new ControlNode[nodeCountX, nodeCountY];

            for(int x = 0; x < nodeCountX; x++ ) 
            {
                for(int y = 0; y < nodeCountY; y++ ) 
                {
                    Vector3 position = new Vector3();
                    position = new Vector3(-mapWidth/2 + x * squareSize + squareSize/2, height[x,y], -mapHeight/2 + y * squareSize + squareSize/2);
                    controlNodesPlatforms[x,y] = new ControlNode(position, map[x,y] == 0, squareSize);
                }
            }

            squares = new Square[nodeCountX -1, nodeCountY -1];
            for(int x = 0; x < nodeCountX -1; x++ ) 
            {
                for(int y = 0; y < nodeCountY -1; y++ ) 
                {
                    squares[x,y] = new Square(controlNodesPlatforms[x,y+1], controlNodesPlatforms[x+1,y+1], controlNodesPlatforms[x+1,y], controlNodesPlatforms[x,y]);
                }
            }           
        }
    }

    public class Square 
    {
        public ControlNode topLeft, topRight, bottomLeft, bottomRight;
        public Node centreTop, centreRight, centreBottom, centreLeft;
        public int configuration;
        public Square(ControlNode _topLeft, ControlNode _topRight, ControlNode _bottomRight, ControlNode _bottomLeft) {
            topLeft = _topLeft;
            topRight = _topRight;
            bottomLeft = _bottomLeft;
            bottomRight = _bottomRight;

            centreTop = topLeft.right;
            centreRight = bottomRight.above;
            centreBottom = bottomLeft.right;
            centreLeft = bottomLeft.above;

            if(topLeft.active)
                configuration +=8;

            if(topRight.active)
                configuration +=4;

            if(bottomRight.active)
                configuration +=2;

            if(bottomLeft.active)
                configuration +=1;
        }
    }

    public class Node 
    {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 _pos) 
        {
            position = _pos;
        }
    }

    public class ControlNode : Node 
    {
        public bool active;
        public Node above, right;

        public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos) 
        {
            active = _active;
            above = new Node(position + Vector3.forward * squareSize/2f);
            right = new Node(position + Vector3.right * squareSize/2f);
        }
    }
}
