using UnityEngine;

public class GPUTwoTriangles : MonoBehaviour
{
    public ComputeShader computeShader;
    public Material material;
    public int triangleCount = 2;
    public int actualCount;

    private ComputeBuffer triangleBuffer;
    private ComputeBuffer countBuffer;

    struct Triangle
    {
        public Vector3 v0, v1, v2;
        public Vector2 uv0, uv1, uv2;
    }

    void Start()
    {
        int stride = sizeof(float) * (3 * 3 + 3 * 2);
        triangleBuffer = new ComputeBuffer(triangleCount, stride, ComputeBufferType.Append);
        triangleBuffer.SetCounterValue(0);

        int kernel = computeShader.FindKernel("CSMain");
        computeShader.SetBuffer(kernel, "triangleBuffer", triangleBuffer);
        computeShader.Dispatch(kernel, 1, 1, 1);

        countBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        ComputeBuffer.CopyCount(triangleBuffer, countBuffer, 0);
        int[] countArray = new int[1];
        countBuffer.GetData(countArray);
        actualCount = countArray[0];

        material.SetBuffer("triangleBuffer", triangleBuffer);
    }

    void OnRenderObject()
    {
        material.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Triangles, actualCount * 3);
    }

    void OnDestroy()
    {
        triangleBuffer?.Release();
        countBuffer?.Release();
    }
}