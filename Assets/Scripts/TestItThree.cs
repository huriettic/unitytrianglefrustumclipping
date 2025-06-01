using System.Collections.Generic;
using UnityEngine;

public class TestItThree : MonoBehaviour
{
    private int start;

    private int end;

    public int triCount;

    private float[] d;

    private bool[] ins;

    public Vector3[] VerticesArray;

    public Vector2[] TexturesArray;

    public Vector3[] NormalsArray;

    public int[] IndicesArray;

    public Vector3 camPosition;

    public Plane[] planes;

    public Mesh originalmesh;

    public List<Vector3> OriginalVertices = new List<Vector3>();

    public List<Vector3> OriginalVerticesWorld = new List<Vector3>();

    public List<Vector2> OriginalTextures = new List<Vector2>();

    public List<Vector3> OriginalNormals = new List<Vector3>();

    public List<int> OriginalTriangles = new List<int>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        camPosition = Camera.main.transform.position;

        planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        originalmesh = this.GetComponent<MeshFilter>().mesh;

        originalmesh.GetVertices(OriginalVertices);
        originalmesh.GetUVs(0, OriginalTextures);
        originalmesh.GetNormals(OriginalNormals);
        originalmesh.GetTriangles(OriginalTriangles, 0);

        for (int i = 0; i < OriginalVertices.Count; i++)
        {
            OriginalVerticesWorld.Add(this.transform.TransformPoint(OriginalVertices[i]));
        }

        TestFunctionTwo(OriginalVerticesWorld, OriginalTextures, OriginalNormals, OriginalTriangles, planes, camPosition);

        GameObject ClippedObject = new GameObject("Clipped");

        ClippedObject.AddComponent<MeshFilter>();
        ClippedObject.AddComponent<MeshRenderer>();

        Renderer ClippedRend = ClippedObject.GetComponent<Renderer>();
        ClippedRend.sharedMaterial = this.GetComponent<Renderer>().sharedMaterial;

        Mesh clippedmesh = new Mesh();

        clippedmesh.SetVertices(VerticesArray);
        clippedmesh.SetUVs(0, TexturesArray);
        clippedmesh.SetNormals(NormalsArray);
        clippedmesh.SetTriangles(IndicesArray, 0, true);

        ClippedObject.GetComponent<MeshFilter>().mesh = clippedmesh;
    }

    public void TestFunctionTwo(List<Vector3> vertices, List<Vector2> textures, List<Vector3> normals, List<int> triangles, Plane[] planes, Vector3 CamPosition)
    {
        VerticesArray = new Vector3[triangles.Count];

        TexturesArray = new Vector2[triangles.Count];

        NormalsArray = new Vector3[triangles.Count];

        d = new float[3];

        ins = new bool[3];

        start = 0;

        end = 0;

        triCount = 0;

        for (int a = 0; a < triangles.Count; a += 3)
        {
            Vector3 Edge1 = vertices[triangles[a + 1]] - vertices[triangles[a]];
            Vector3 Edge2 = vertices[triangles[a + 2]] - vertices[triangles[a]];
            Vector3 Normal = Vector3.Cross(Edge1, Edge2).normalized;
            Vector3 CamDirection = (CamPosition - vertices[triangles[a]]).normalized;
            float triangleDirection = Vector3.Dot(Normal, CamDirection);

            if (triangleDirection < 0)
            {
                continue;
            }

            VerticesArray[start] = vertices[triangles[a]];
            VerticesArray[start + 1] = vertices[triangles[a + 1]];
            VerticesArray[start + 2] = vertices[triangles[a + 2]];

            TexturesArray[start] = textures[triangles[a]];
            TexturesArray[start + 1] = textures[triangles[a + 1]];
            TexturesArray[start + 2] = textures[triangles[a + 2]];

            NormalsArray[start] = normals[triangles[a]];
            NormalsArray[start + 1] = normals[triangles[a + 1]];
            NormalsArray[start + 2] = normals[triangles[a + 2]];

            triCount += 3;

            end = triCount;

            for (int b = 0; b < planes.Length; b++)
            {
                for (int c = start; c < end; c += 3)
                {
                    d[0] = planes[b].GetDistanceToPoint(VerticesArray[c]);
                    d[1] = planes[b].GetDistanceToPoint(VerticesArray[c + 1]);
                    d[2] = planes[b].GetDistanceToPoint(VerticesArray[c + 2]);
                    ins[0] = d[0] > 0;
                    ins[1] = d[1] > 0;
                    ins[2] = d[2] > 0;

                    int inc = 0;

                    int ini = 0;
                    int out1 = 0;
                    int out2 = 0;

                    int outi = 0;
                    int ini1 = 0;
                    int ini2 = 0;

                    if (ins[0])
                    {
                        inc++;
                    }

                    if (ins[1])
                    {
                        inc++;
                    }

                    if (ins[2])
                    {
                        inc++;
                    }

                    if (inc == 0)
                    {
                        VerticesArray[c] = VerticesArray[end];
                        VerticesArray[c + 1] = VerticesArray[end + 1];
                        VerticesArray[c + 2] = VerticesArray[end + 2];

                        TexturesArray[c] = TexturesArray[end];
                        TexturesArray[c + 1] = TexturesArray[end + 1];
                        TexturesArray[c + 2] = TexturesArray[end + 2];

                        NormalsArray[c] = NormalsArray[end];
                        NormalsArray[c + 1] = NormalsArray[end + 1];
                        NormalsArray[c + 2] = NormalsArray[end + 2];

                        triCount -= 3;
                    }
                    else if (inc == 1)
                    {
                        if (ins[0] && !ins[1] && !ins[2])
                        {
                            ini = 0;
                            out1 = 1;
                            out2 = 2;
                        }
                        else if (!ins[0] && ins[1] && !ins[2])
                        {
                            out1 = 2;
                            ini = 1;
                            out2 = 0;
                        }
                        else if (!ins[0] && !ins[1] && ins[2])
                        {
                            out1 = 0;
                            out2 = 1;
                            ini = 2;
                        }

                        float d1 = d[ini] / (d[ini] - d[out1]);
                        float d2 = d[ini] / (d[ini] - d[out2]);

                        Vector3 v0 = VerticesArray[c + ini];
                        Vector3 v1 = Vector3.Lerp(VerticesArray[c + ini], VerticesArray[c + out1], d1);
                        Vector3 v2 = Vector3.Lerp(VerticesArray[c + ini], VerticesArray[c + out2], d2);

                        Vector2 t0 = TexturesArray[c + ini];
                        Vector2 t1 = Vector2.Lerp(TexturesArray[c + ini], TexturesArray[c + out1], d1);
                        Vector2 t2 = Vector2.Lerp(TexturesArray[c + ini], TexturesArray[c + out2], d2);

                        Vector3 n0 = NormalsArray[c + ini];
                        Vector3 n1 = Vector3.Lerp(NormalsArray[c + ini], NormalsArray[c + out1], d1).normalized;
                        Vector3 n2 = Vector3.Lerp(NormalsArray[c + ini], NormalsArray[c + out2], d2).normalized;

                        VerticesArray[c] = v0;
                        VerticesArray[c + 1] = v1;
                        VerticesArray[c + 2] = v2;

                        TexturesArray[c] = t0;
                        TexturesArray[c + 1] = t1;
                        TexturesArray[c + 2] = t2;

                        NormalsArray[c] = n0;
                        NormalsArray[c + 1] = n1;
                        NormalsArray[c + 2] = n2;
                    }
                    else if (inc == 2)
                    {
                        if (!ins[0] && ins[1] && ins[2])
                        {
                            outi = 0;
                            ini1 = 1;
                            ini2 = 2;
                        }
                        else if (ins[0] && !ins[1] && ins[2])
                        {
                            ini1 = 2;
                            outi = 1;
                            ini2 = 0;
                        }
                        else if (ins[0] && ins[1] && !ins[2])
                        {
                            ini1 = 0;
                            ini2 = 1;
                            outi = 2;
                        }

                        float d1 = d[ini1] / (d[ini1] - d[outi]);
                        float d2 = d[ini2] / (d[ini2] - d[outi]);

                        Vector3 vedge1 = Vector3.Lerp(VerticesArray[c + ini1], VerticesArray[c + outi], d1);
                        Vector3 vedge2 = Vector3.Lerp(VerticesArray[c + ini2], VerticesArray[c + outi], d2);

                        Vector3 tedge1 = Vector2.Lerp(TexturesArray[c + ini1], TexturesArray[c + outi], d1);
                        Vector3 tedge2 = Vector2.Lerp(TexturesArray[c + ini2], TexturesArray[c + outi], d2);

                        Vector3 nedge1 = Vector3.Lerp(NormalsArray[c + ini1], NormalsArray[c + outi], d1).normalized;
                        Vector3 nedge2 = Vector3.Lerp(NormalsArray[c + ini2], NormalsArray[c + outi], d2).normalized;

                        Vector3 t1v0 = VerticesArray[c + ini1];
                        Vector3 t1v1 = VerticesArray[c + ini2];
                        Vector3 t1v2 = vedge1;

                        Vector3 t2v0 = vedge1;
                        Vector3 t2v1 = VerticesArray[c + ini2];
                        Vector3 t2v2 = vedge2;

                        Vector2 t1t0 = TexturesArray[c + ini1];
                        Vector2 t1t1 = TexturesArray[c + ini2];
                        Vector2 t1t2 = tedge1;

                        Vector2 t2t0 = tedge1;
                        Vector2 t2t1 = TexturesArray[c + ini2];
                        Vector2 t2t2 = tedge2;

                        Vector3 t1n0 = NormalsArray[c + ini1];
                        Vector3 t1n1 = NormalsArray[c + ini2];
                        Vector3 t1n2 = nedge1;

                        Vector3 t2n0 = nedge1;
                        Vector3 t2n1 = NormalsArray[c + ini2];
                        Vector3 t2n2 = nedge2;

                        VerticesArray[c] = t1v0;
                        VerticesArray[c + 1] = t1v1;
                        VerticesArray[c + 2] = t1v2;

                        VerticesArray[end] = t2v0;
                        VerticesArray[end + 1] = t2v1;
                        VerticesArray[end + 2] = t2v2;

                        TexturesArray[c] = t1t0;
                        TexturesArray[c + 1] = t1t1;
                        TexturesArray[c + 2] = t1t2;

                        TexturesArray[end] = t2t0;
                        TexturesArray[end + 1] = t2t1;
                        TexturesArray[end + 2] = t2t2;

                        NormalsArray[c] = t1n0;
                        NormalsArray[c + 1] = t1n1;
                        NormalsArray[c + 2] = t1n2;

                        NormalsArray[end] = t2n0;
                        NormalsArray[end + 1] = t2n1;
                        NormalsArray[end + 2] = t2n2;

                        triCount += 3;
                    }
                    else if (inc == 3)
                    {
                        continue;
                    }
                }

                end = triCount;
            }

            start = end;
        }

        IndicesArray = new int[triCount];

        for (int i = 0; (i < triCount); i++)
        {
            IndicesArray[i] = i;
        }
    }
}
