using System.Collections.Generic;
using UnityEngine;

public class TestItFive : MonoBehaviour
{
    public Camera Cam;
    public ComputeShader computeShader;

    public List<Vector3> OriginalVertices = new List<Vector3>();
    public List<Vector3> OriginalVerticesWorld = new List<Vector3>();
    public List<Vector2> OriginalTextures = new List<Vector2>();
    public List<int> OriginalTriangles = new List<int>();

    private Material material;
    private bool isVisible;
    private Vector3 camPosition;
    private Renderer rend;
    private CameraMoved camMoved;
    private Mesh originalmesh;
    private Plane[] planes;
    private Vector4[] planevectors;
    private int kernel;
    private int threadCount;
    private ComputeBuffer processVertices;
    private ComputeBuffer processTextures;
    private ComputeBuffer processBool;
    private ComputeBuffer temporaryVertices;
    private ComputeBuffer temporaryTextures;
    private ComputeBuffer vertexBuffer;
    private ComputeBuffer textureBuffer;
    private ComputeBuffer indicesBuffer;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer argsBuffer;

    struct Triangle
    {
        public Vector3 v0, v1, v2;
        public Vector2 uv0, uv1, uv2;
    }

    void Start()
    {
        planevectors = new Vector4[6];

        originalmesh = this.GetComponent<MeshFilter>().mesh;

        originalmesh.GetVertices(OriginalVertices);
        originalmesh.GetUVs(0, OriginalTextures);
        originalmesh.GetTriangles(OriginalTriangles, 0);

        rend = this.GetComponent<Renderer>();

        camMoved = Cam.GetComponent<CameraMoved>();

        threadCount = OriginalTriangles.Count / 3;

        int strideTriangle = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Triangle));
        int strideVertex = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector3));
        int strideTexture = System.Runtime.InteropServices.Marshal.SizeOf(typeof(Vector2));
        int strideBool = System.Runtime.InteropServices.Marshal.SizeOf(typeof(bool));
        int strideInt = System.Runtime.InteropServices.Marshal.SizeOf(typeof(int));
        int strideUint = System.Runtime.InteropServices.Marshal.SizeOf(typeof(uint));
        int scratchSize = threadCount * 256;

        processVertices = new ComputeBuffer(scratchSize, strideVertex);
        processTextures = new ComputeBuffer(scratchSize, strideTexture);
        processBool = new ComputeBuffer(scratchSize, strideBool);
        temporaryVertices = new ComputeBuffer(scratchSize, strideVertex);
        temporaryTextures = new ComputeBuffer(scratchSize, strideTexture);
        vertexBuffer = new ComputeBuffer(OriginalVertices.Count, strideVertex, ComputeBufferType.Structured);
        textureBuffer = new ComputeBuffer(OriginalTextures.Count, strideTexture, ComputeBufferType.Structured);
        indicesBuffer = new ComputeBuffer(OriginalTriangles.Count, strideInt, ComputeBufferType.Structured);
        triangleBuffer = new ComputeBuffer(OriginalTriangles.Count / 3, strideTriangle, ComputeBufferType.Append);
        argsBuffer = new ComputeBuffer(1, strideUint * 4, ComputeBufferType.IndirectArguments);

        textureBuffer.SetData(OriginalTextures);
        indicesBuffer.SetData(OriginalTriangles);

        kernel = computeShader.FindKernel("CSMain");

        material = new Material(this.GetComponent<MeshRenderer>().sharedMaterial);
    }

    void Update()
    {
        camPosition = Cam.transform.position;

        planes = GeometryUtility.CalculateFrustumPlanes(Cam);

        isVisible = GeometryUtility.TestPlanesAABB(planes, rend.bounds);

        if (isVisible)
        {
            if (this.transform.hasChanged || camMoved.TransformChanged)
            {
                for (int i = 0; i < 6; i++)
                {
                    planevectors[i] = new Vector4(planes[i].normal.x, planes[i].normal.y, planes[i].normal.z, planes[i].distance);
                }

                OriginalVerticesWorld.Clear();

                for (int i = 0; i < OriginalVertices.Count; i++)
                {
                    OriginalVerticesWorld.Add(this.transform.TransformPoint(OriginalVertices[i]));
                }

                vertexBuffer.SetData(OriginalVerticesWorld);

                argsBuffer.SetData(new uint[] { 0, 1, 0, 0 });

                triangleBuffer.SetCounterValue(0);

                computeShader.SetBuffer(kernel, "processVertices", processVertices);
                computeShader.SetBuffer(kernel, "processTextures", processTextures);
                computeShader.SetBuffer(kernel, "processBool", processBool);
                computeShader.SetBuffer(kernel, "temporaryVertices", temporaryVertices);
                computeShader.SetBuffer(kernel, "temporaryTextures", temporaryTextures);
                computeShader.SetBuffer(kernel, "vertexBuffer", vertexBuffer);
                computeShader.SetBuffer(kernel, "textureBuffer", textureBuffer);
                computeShader.SetBuffer(kernel, "indicesBuffer", indicesBuffer);
                computeShader.SetBuffer(kernel, "triangleBuffer", triangleBuffer);
                computeShader.SetBuffer(kernel, "argsBuffer", argsBuffer);
                computeShader.SetVector("CamPosition", new Vector4(camPosition.x, camPosition.y, camPosition.z, 1.0f));
                computeShader.SetVectorArray("planes", planevectors);

                computeShader.Dispatch(kernel, threadCount, 1, 1);

                material.SetBuffer("triangleBuffer", triangleBuffer);

                this.transform.hasChanged = false;
            }
        }
    }

    void OnRenderObject()
    {
        if (!isVisible)
        {
            return;
        }
         
        material.SetPass(0);
        Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, argsBuffer);
    }

    void OnDestroy()
    {
        processVertices?.Release();
        processTextures?.Release();
        processBool?.Release();
        temporaryVertices?.Release();
        temporaryTextures?.Release();
        vertexBuffer?.Release();
        textureBuffer?.Release();
        indicesBuffer?.Release();
        triangleBuffer?.Release();
        argsBuffer?.Release();
    }
}