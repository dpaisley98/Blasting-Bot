using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGenerator : MonoBehaviour {
    public SquareGrid squareGrid;
    List<Vector3> vertices;
    List<int> triangles;

    Dictionary<int, List<Triangle>> triangleDict = new Dictionary<int, List<Triangle>>();
    List<GameObject> roomObjects = new List<GameObject>();

    List<List<int>> outlines = new List<List<int>>();
    HashSet<int> checkedVertices = new HashSet<int>();

    public MeshFilter walls;
    public MeshFilter level;

    public void GenerateMesh(int[,] map, float squareSize){
        triangleDict.Clear();
        outlines.Clear();
        checkedVertices.Clear();
        squareGrid = new SquareGrid(map, squareSize);
        vertices = new List<Vector3>();
        triangles = new List<int>();

        for(int x = 0; x < squareGrid.squares.GetLength(0); x++ ) {
            for(int y = 0; y < squareGrid.squares.GetLength(1); y++ ) { 
                TriangulateSquares(squareGrid.squares[x,y]);
            }
        }

        Mesh mesh = new Mesh();
        level.mesh = mesh;
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        int tileAmount = 10;
        Vector2[] uvs = new Vector2[vertices.Count];
        for (int i = 0; i < vertices.Count; i++) {
            float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].x) * tileAmount;
            float percentY = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, vertices[i].z) * tileAmount;

            uvs[i] = new Vector2(percentX, percentY);
        }

        mesh.uv = uvs;
        MeshCollider currentCollider = level.gameObject.GetComponent<MeshCollider>();
        Destroy(currentCollider);
        MeshCollider levelCollider = level.gameObject.AddComponent<MeshCollider>();
        levelCollider.sharedMesh = mesh;

        CreateWallMesh();
    }

    /*void GenerateRoomMeshes(List<int[,,]> maps, float squareSize) {
        // Destroy any previously generated room GameObjects
        foreach (GameObject roomObject in roomObjects)
        {
            Destroy(roomObject);
        }
        roomObjects.Clear();

        // Create a new GameObject for each room
        foreach (int[,,] roomMap in maps) 
        {
            GameObject roomObject = new GameObject("Roo");
            MeshFilter mf = roomObject.AddComponent<MeshFilter>();
            MeshRenderer mr = roomObject.AddComponent<MeshRenderer>();

            // Create a new mesh for the room
            Mesh roomMesh = new Mesh();
            mf.mesh = roomMesh;

            List<Vector3> roomVertices = roomMap;
            List<int> roomTriangles = new List<int>();
            List<Vector2> roomUVs = new List<Vector2>();

            // Create triangles and UVs for the room mesh
            for (int j = 0; j < roomVertices.Count; j++)
            {
                roomTriangles.Add(j);
                roomUVs.Add(new Vector2(roomVertices[j].x, roomVertices[j].z));
            }

            roomMesh.vertices = roomVertices.ToArray();
            roomMesh.triangles = roomTriangles.ToArray();
            roomMesh.uv = roomUVs.ToArray();

            // Set the material for the room mesh renderer
            mr.material = roomMaterial;

            // Add the generated room GameObject to the list
            roomObjects.Add(roomObject);
        }
    }*/

    void CreateWallMesh() {
        MeshCollider currentCollider = walls.gameObject.GetComponent<MeshCollider> ();
        Destroy(currentCollider);

        CalculateMeshOutlines();
        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();
        Mesh wallMesh = new Mesh();
        float wallHeight = 5;

        foreach(List<int> outline in outlines) {
            for (int i =0; i < outline.Count -1; i++) {
                int startIndex = wallVertices.Count;
                wallVertices.Add(vertices[outline[i]]);
                wallVertices.Add(vertices[outline[i+1]]);
                wallVertices.Add(vertices[outline[i]] + Vector3.up * wallHeight);
                wallVertices.Add(vertices[outline[i+1]] + Vector3.up * wallHeight);

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

        MeshCollider wallCollider = walls.gameObject.AddComponent<MeshCollider>();
        wallCollider.sharedMesh = wallMesh;
    }
    //create opposite config for other mesh, pass two arrays to the meshfrompoints, then create two triangle configs per if statement and assign vertices to two different vertices arrays 
    void TriangulateSquares(Square square) {
        switch(square.configuration) {
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

    void MeshFromPoints(params Node[] points) {
        AssignVertices(points);
        if(points.Length >= 3) {
            CreateTriangle(points[0], points[1], points[2]);
        }
        if(points.Length >= 4) {
            CreateTriangle(points[0], points[2], points[3]);
        }
        if(points.Length >= 5) {
            CreateTriangle(points[0], points[3], points[4]);
        }
        if(points.Length >= 6) {
            CreateTriangle(points[0], points[4], points[5]);
        }
    }

    void AssignVertices(Node[] points) {
        for(int i = 0; i < points.Length; i++) {
            if(points[i].vertexIndex == -1) {
                points[i].vertexIndex = vertices.Count;
                vertices.Add(points[i].position);
            }
        }
    }

    struct Triangle {
        public int vertexIndexA;                
        public int vertexIndexB;
        public int vertexIndexC;

        int[] vertices;

        public Triangle (int a, int b, int c) {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }

        public int this[int i] {
            get {
                return vertices[i];
            }
        }

        public bool Contains(int vertexIndex) {
            return vertexIndexA == vertexIndex || vertexIndexB == vertexIndex || vertexIndexC == vertexIndex;
        }
    }

    void CreateTriangle(Node a, Node b, Node c) {
        triangles.Add(a.vertexIndex);
        triangles.Add(b.vertexIndex);
        triangles.Add(c.vertexIndex);

        Triangle triangle = new Triangle (a.vertexIndex, b.vertexIndex, c.vertexIndex);
        AddTriangleToDictinary(triangle.vertexIndexA, triangle);
        AddTriangleToDictinary(triangle.vertexIndexB, triangle);
        AddTriangleToDictinary(triangle.vertexIndexC, triangle);
    }

    void AddTriangleToDictinary(int vertextIndexKey, Triangle triangle) {
        if(triangleDict.ContainsKey(vertextIndexKey)) {
            triangleDict[vertextIndexKey].Add(triangle);
        } else {
            List<Triangle> triangleList = new List<Triangle>();
            triangleList.Add(triangle);
            triangleDict.Add(vertextIndexKey, triangleList);
        }
    }

    void CalculateMeshOutlines() {
        for(int vertexI = 0; vertexI < vertices.Count; vertexI++) {
            if(!checkedVertices.Contains(vertexI)){
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

    void FollowOutline(int vertexIndex, int outlineIndex) {
        outlines[outlineIndex].Add(vertexIndex);
        checkedVertices.Add(vertexIndex);
        int nextVertexIndex = GetConnectedOutlineVertex(vertexIndex);

        if(nextVertexIndex != -1) {
            FollowOutline(nextVertexIndex, outlineIndex);
        }
    }
    
    int GetConnectedOutlineVertex(int vertexIndex) {
        List<Triangle> trianglesContainigVertex = triangleDict[vertexIndex];

        for(int i = 0; i < trianglesContainigVertex.Count; i++) { 
            Triangle triangle = trianglesContainigVertex[i];

            for(int j = 0; j < 3; j++) {
                int vertexB = triangle[j];
                if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB)){
                    if(IsOutlineEdge(vertexIndex, vertexB)) {
                        return vertexB;
                    }
                }
            }
        }

        return -1;
    }

    bool IsOutlineEdge(int vertexA, int vertexB) {
        List<Triangle> trianglesContainigVertexA = triangleDict[vertexA];
        int sharedTriangleCount = 0;

        for(int i = 0; i < trianglesContainigVertexA.Count; i++) {
            if(trianglesContainigVertexA[i].Contains(vertexB)) {
                sharedTriangleCount++;
                if(sharedTriangleCount > 1){
                    break;
                }
            }
        }
        return sharedTriangleCount == 1;
    }

    void OnDrawGizmos() {
/*         if(squareGrid != null) {
            for(int x = 0; x < squareGrid.squares.GetLength(0); x++ ) {
                for(int y = 0; y < squareGrid.squares.GetLength(1); y++ ) { 
                    Gizmos.color = (squareGrid.squares[x,y].topLeft.active)?Color.black:Color.white; 
                    Gizmos.DrawCube(squareGrid.squares[x,y].topLeft.position, Vector3.one * .4f);

                    Gizmos.color = (squareGrid.squares[x,y].topRight.active)?Color.black:Color.white; 
                    Gizmos.DrawCube(squareGrid.squares[x,y].topRight.position, Vector3.one * .4f); 

                    Gizmos.color = (squareGrid.squares[x,y].bottomRight.active)?Color.black:Color.white; 
                    Gizmos.DrawCube(squareGrid.squares[x,y].bottomRight.position, Vector3.one * .4f);

                    Gizmos.color = (squareGrid.squares[x,y].bottomLeft.active)?Color.black:Color.white; 
                    Gizmos.DrawCube(squareGrid.squares[x,y].bottomLeft.position, Vector3.one * .4f); 

                    Gizmos.color = Color.gray;
                    Gizmos.DrawCube(squareGrid.squares[x,y].centreTop.position, Vector3.one * .15f);
                    Gizmos.DrawCube(squareGrid.squares[x,y].centreBottom.position, Vector3.one * .15f);
                    Gizmos.DrawCube(squareGrid.squares[x,y].centreLeft.position, Vector3.one * .15f);
                    Gizmos.DrawCube(squareGrid.squares[x,y].centreRight.position, Vector3.one * .15f);

                }
            }
        } */
    }

    public class SquareGrid {
        public Square[,] squares;

        public SquareGrid(int[,] map, float squareSize, float height = 0) {
            int nodeCountX = map.GetLength(0);
            int nodeCountY = map.GetLength(1);
            float mapWidth = nodeCountX * squareSize;
            float mapHeight = nodeCountY * squareSize;

            ControlNode[,] controlNodes = new ControlNode[nodeCountX, nodeCountY];

            for(int x = 0; x < nodeCountX; x++ ) {
                for(int y = 0; y < nodeCountY; y++ ) {
                    Vector3 position = new Vector3(-mapWidth/2 + x * squareSize + squareSize/2, height, -mapHeight/2 + y * squareSize + squareSize/2);
                    controlNodes[x,y] = new ControlNode(position, map[x,y] == 1, squareSize);
                }
            }

            squares = new Square[nodeCountX -1, nodeCountY -1];
            for(int x = 0; x < nodeCountX -1; x++ ) {
                for(int y = 0; y < nodeCountY -1; y++ ) {
                    squares[x,y] = new Square(controlNodes[x,y+1], controlNodes[x+1,y+1], controlNodes[x+1,y], controlNodes[x,y]);
                }
            }           
        }
    }

    public class Square {
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

    public class Node {
        public Vector3 position;
        public int vertexIndex = -1;

        public Node(Vector3 _pos) {
            position = _pos;
        }
    }

    public class ControlNode : Node {
        public bool active;
        public Node above, right;

        public ControlNode(Vector3 _pos, bool _active, float squareSize) : base(_pos) {
            active = _active;
            above = new Node(position + Vector3.forward * squareSize/2f);
            right = new Node(position + Vector3.right * squareSize/2f);
        }
    }
}
