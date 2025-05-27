using System.Collections.Generic;
using UnityEngine;

public class GPUTriangleClipping : MonoBehaviour
{
    private Mesh originalmesh;

    private TriangleData[] processedTriangles;

    private Vector3[] processedVertices;

    private Vector2[] processedTextures;

    private Vector3[] processedNormals;

    private int[] processedIndices;

    public List<Vector3> verticeslocal = new List<Vector3>();
    public List<Vector3> vertices = new List<Vector3>();
    public List<Vector2> textures = new List<Vector2>();
    public List<Vector3> normals = new List<Vector3>();
    public List<int> indices = new List<int>();

    public List<TriangleData> inputtriangles = new List<TriangleData>();
    public List<TriangleData> outputtriangles = new List<TriangleData>();

    public List<Vector3> outvertices = new List<Vector3>();
    public List<Vector2> outtextures = new List<Vector2>();
    public List<Vector3> outnormals = new List<Vector3>();
    public List<int> outindices = new List<int>();

    private ComputeShader computeShader1;

    private ComputeShader computeShader2;

    private ComputeShader computeShader3;

    private Vector3 camPosition;

    private Plane[] frustumplanes;

    private int triangleDataSize;

    private uint[] counterInit;

    private uint[] finalCount;

    private int processedCount;

    [System.Serializable]
    public struct TriangleData
    {
        public Vector3 v0, v1, v2;
        public Vector2 t0, t1, t2;
        public Vector3 n0, n1, n2;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        counterInit = new uint[1] { 0 };

        frustumplanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        camPosition = Camera.main.transform.position;

        computeShader1 = Resources.Load<ComputeShader>("ConvertListsToTriangles");

        computeShader2 = Resources.Load<ComputeShader>("ClipTheTriangles");

        computeShader3 = Resources.Load<ComputeShader>("ConvertTrianglesToLists");

        triangleDataSize = (sizeof(float) * 3 * 3) + (sizeof(float) * 2 * 3) + (sizeof(float) * 3 * 3);

        originalmesh = this.GetComponent<MeshFilter>().mesh;

        originalmesh.GetVertices(verticeslocal);
        originalmesh.GetUVs(0, textures);
        originalmesh.GetNormals(normals);
        originalmesh.GetTriangles(indices, 0);

        for (int i = 0; i < verticeslocal.Count; i++)
        {
            vertices.Add(this.transform.TransformPoint(verticeslocal[i]));
        }

        ProcessTrianglesComputeShader(vertices, textures, normals, indices, triangleDataSize, camPosition);

        outputtriangles = ClipTriangles(inputtriangles, frustumplanes, counterInit);

        ConvertTrianglestoLists(outputtriangles, triangleDataSize);

        GameObject ClippedObject = new GameObject("Clipped");

        ClippedObject.AddComponent<MeshFilter>();
        ClippedObject.AddComponent<MeshRenderer>();

        Renderer ClippedRend = ClippedObject.GetComponent<Renderer>();
        ClippedRend.sharedMaterial = this.GetComponent<Renderer>().sharedMaterial;

        Mesh clippedmesh = new Mesh();

        clippedmesh.SetVertices(outvertices);
        clippedmesh.SetUVs(0, outtextures);
        clippedmesh.SetNormals(outnormals);
        clippedmesh.SetTriangles(outindices, 0, true);

        ClippedObject.GetComponent<MeshFilter>().mesh = clippedmesh;
    }

    public void ConvertTrianglestoLists(List<TriangleData> inputTriangles, int TriangleDataSize)
    {
        ComputeBuffer inputBuffer = new ComputeBuffer(inputTriangles.Count, TriangleDataSize);
        ComputeBuffer vertexBuffer = new ComputeBuffer(inputTriangles.Count * 3, sizeof(float) * 3, ComputeBufferType.Structured);
        ComputeBuffer textureBuffer = new ComputeBuffer(inputTriangles.Count * 3, sizeof(float) * 2, ComputeBufferType.Structured);
        ComputeBuffer normalBuffer = new ComputeBuffer(inputTriangles.Count * 3, sizeof(float) * 3, ComputeBufferType.Structured);
        ComputeBuffer indexBuffer = new ComputeBuffer(inputTriangles.Count * 3, sizeof(int) * 3, ComputeBufferType.Structured);
        
        inputBuffer.SetData(inputTriangles);

        computeShader3.SetBuffer(0, "InputTriangles", inputBuffer);
        computeShader3.SetBuffer(0, "VertexBuffer", vertexBuffer);
        computeShader3.SetBuffer(0, "TextureBuffer", textureBuffer);
        computeShader3.SetBuffer(0, "NormalBuffer", normalBuffer);
        computeShader3.SetBuffer(0, "IndexBuffer", indexBuffer);
       
        computeShader3.Dispatch(0, inputTriangles.Count * 3, 1, 1);

        processedCount = vertexBuffer.count;
        processedVertices = new Vector3[processedCount];
        vertexBuffer.GetData(processedVertices);
        outvertices.AddRange(processedVertices);

        processedCount = textureBuffer.count;
        processedTextures = new Vector2[processedCount];
        textureBuffer.GetData(processedTextures);
        outtextures.AddRange(processedTextures);

        processedCount = normalBuffer.count;
        processedNormals = new Vector3[processedCount];
        normalBuffer.GetData(processedNormals);
        outnormals.AddRange(processedNormals);

        processedCount = indexBuffer.count;
        processedIndices = new int[processedCount];
        indexBuffer.GetData(processedIndices);
        outindices.AddRange(processedIndices);

        vertexBuffer.Release();
        textureBuffer.Release();
        normalBuffer.Release();
        indexBuffer.Release();
        inputBuffer.Release();
    }

    public List<TriangleData> ClipTriangles(List<TriangleData> triangles, Plane[] planes, uint[] counter)
    {
        ComputeBuffer inputBuffer = new ComputeBuffer(triangles.Count * 2, triangleDataSize);
        ComputeBuffer outputBuffer = new ComputeBuffer(triangles.Count * 2, triangleDataSize, ComputeBufferType.Append);
        ComputeBuffer triangleCounterBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        for (int i = 0; i < planes.Length; i++)
        {
            inputBuffer.SetData(triangles);
            outputBuffer.SetCounterValue(0);
            triangleCounterBuffer.SetData(counter);

            computeShader2.SetBuffer(0, "InputTriangles", inputBuffer);
            computeShader2.SetBuffer(0, "OutputTriangles", outputBuffer);
            computeShader2.SetBuffer(0, "TriangleCounter", triangleCounterBuffer);

            computeShader2.SetVector("Plane", new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance));

            computeShader2.Dispatch(0, triangles.Count, 1, 1);

            finalCount = new uint[1];

            triangleCounterBuffer.GetData(finalCount);

            processedCount = (int)finalCount[0];

            processedTriangles = new TriangleData[processedCount];

            outputBuffer.GetData(processedTriangles);

            triangles.Clear();

            triangles.AddRange(processedTriangles);
        }

        inputBuffer.Release();
        outputBuffer.Release();
        triangleCounterBuffer.Release();

        return triangles;
    }

    public void ProcessTrianglesComputeShader(List<Vector3> Vertices,List<Vector2> Textures, List<Vector3> Normals, List<int> Indices, int TriangleDataSize, Vector3 CamPos)
    {
        ComputeBuffer vertexBuffer = new ComputeBuffer(Vertices.Count, sizeof(float) * 3);
        ComputeBuffer textureBuffer = new ComputeBuffer(Textures.Count, sizeof(float) * 2);
        ComputeBuffer normalBuffer = new ComputeBuffer(Normals.Count, sizeof(float) * 3);
        ComputeBuffer indexBuffer = new ComputeBuffer(Indices.Count, sizeof(int));
        ComputeBuffer outputBuffer = new ComputeBuffer(Indices.Count / 3, TriangleDataSize, ComputeBufferType.Append);
        ComputeBuffer triangleCounterBuffer = new ComputeBuffer(1, sizeof(uint), ComputeBufferType.Raw);

        vertexBuffer.SetData(Vertices);
        textureBuffer.SetData(Textures);
        normalBuffer.SetData(Normals);
        indexBuffer.SetData(Indices);
        outputBuffer.SetCounterValue(0);
        triangleCounterBuffer.SetData(counterInit);

        computeShader1.SetBuffer(0, "VertexBuffer", vertexBuffer);
        computeShader1.SetBuffer(0, "TextureBuffer", textureBuffer);
        computeShader1.SetBuffer(0, "NormalBuffer", normalBuffer);
        computeShader1.SetBuffer(0, "IndexBuffer", indexBuffer);
        computeShader1.SetBuffer(0, "OutputTriangles", outputBuffer);
        computeShader1.SetBuffer(0, "TriangleCounter", triangleCounterBuffer);
        computeShader1.SetVector("CamPosition", new Vector4(CamPos.x, CamPos.y, CamPos.z, 1.0f));

        computeShader1.Dispatch(0, Indices.Count / 3, 1, 1);

        finalCount = new uint[1];

        triangleCounterBuffer.GetData(finalCount);

        processedCount = (int)finalCount[0];

        processedTriangles = new TriangleData[processedCount];

        outputBuffer.GetData(processedTriangles);

        inputtriangles.AddRange(processedTriangles);

        vertexBuffer.Release();
        textureBuffer.Release();
        normalBuffer.Release();
        indexBuffer.Release();
        outputBuffer.Release();
        triangleCounterBuffer.Release();
    }
}
