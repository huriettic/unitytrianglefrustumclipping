using System.Collections.Generic;
using UnityEngine;

public class TestItFive : MonoBehaviour
{
    public Camera Cam;
    private RenderParams rp;
    private Matrix4x4 matrix;
    private Mesh clippedmesh;
    private Mesh originalmesh;
    private Material material;
    private Vector3 camPosition;
    private Plane[] planes;
    private int[] processbool;
    private Vector3[] processvertices;
    private Vector4[] processtextures;
    private Vector3[] processnormals;
    private Vector3[] temporaryvertices;
    private Vector4[] temporarytextures;
    private Vector3[] temporarynormals;
    public List<Vector3> OriginalVertices = new List<Vector3>();
    public List<Vector3> OriginalVerticesWorld = new List<Vector3>();
    public List<Vector2> OriginalTextures = new List<Vector2>();
    public List<Vector3> OriginalNormals = new List<Vector3>();
    public List<int> OriginalTriangles = new List<int>();
    private List<bool> ProcessBool = new List<bool>();

    private List<Vector3> ProcessVertices = new List<Vector3>();

    private List<Vector4> ProcessTextures = new List<Vector4>();
    private List<Vector3> ProcessNormals = new List<Vector3>();

    private List<Vector3> TemporaryVertices = new List<Vector3>();

    private List<Vector4> TemporaryTextures = new List<Vector4>();
    private List<Vector3> TemporaryNormals = new List<Vector3>();

    public List<Vector3> OutVertices = new List<Vector3>();
    public List<Vector3> OutVerticesLocal = new List<Vector3>();
    public List<Vector4> OutTextures = new List<Vector4>();
    public List<Vector3> OutNormals = new List<Vector3>();
    public List<int> OutTriangles = new List<int>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalmesh = this.GetComponent<MeshFilter>().mesh;

        originalmesh.GetUVs(0, OriginalTextures);
        originalmesh.GetNormals(OriginalNormals);
        originalmesh.GetTriangles(OriginalTriangles, 0);

        processbool = new int[OriginalTriangles.Count];
        processvertices = new Vector3[OriginalTriangles.Count];
        processtextures = new Vector4[OriginalTriangles.Count];
        processnormals = new Vector3[OriginalTriangles.Count];
        temporaryvertices = new Vector3[OriginalTriangles.Count];
        temporarytextures = new Vector4[OriginalTriangles.Count];
        temporarynormals = new Vector3[OriginalTriangles.Count];

        clippedmesh = new Mesh();

        material = this.GetComponent<MeshRenderer>().sharedMaterial;

        rp = new RenderParams();
    }

    void Update()
    {
        camPosition = Cam.transform.position;

        planes = GeometryUtility.CalculateFrustumPlanes(Cam);

        if (GeometryUtility.TestPlanesAABB(planes, this.GetComponent<Renderer>().bounds))
        {
            if (this.transform.hasChanged || Cam.GetComponent<CameraMoved>().TransformChanged)
            {
                OriginalVertices.Clear();
                OriginalVerticesWorld.Clear();
                OutVerticesLocal.Clear();

                originalmesh.GetVertices(OriginalVertices);

                for (int i = 0; i < OriginalVertices.Count; i++)
                {
                    OriginalVerticesWorld.Add(this.transform.TransformPoint(OriginalVertices[i]));
                }

                (List<Vector3>, List<Vector4>, List<Vector3>, List<int>) Clipped = ClipTrianglesWithPlanesTwo(OriginalVerticesWorld, OriginalTextures, OriginalNormals, OriginalTriangles, planes, camPosition);

                for (int i = 0; i < Clipped.Item1.Count; i++)
                {
                    OutVerticesLocal.Add(this.transform.InverseTransformPoint(Clipped.Item1[i]));
                }

                clippedmesh.Clear();

                clippedmesh.SetVertices(OutVerticesLocal);
                clippedmesh.SetUVs(0, Clipped.Item2);
                clippedmesh.SetTriangles(Clipped.Item4, 0, true);
                clippedmesh.SetNormals(Clipped.Item3);

                this.transform.hasChanged = false;
            }

            rp.material = material;

            matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.lossyScale);

            Graphics.RenderMesh(rp, clippedmesh, 0, matrix);
        }
    }

    public (List<Vector3>, List<Vector4>, List<Vector3>, List<int>) ClipTrianglesWithPlanesTwo(List<Vector3> vertices, List<Vector2> textures, List<Vector3> normals, List<int> triangles, Plane[] planes, Vector3 CamPosition)
    {
        OutVertices.Clear();
        OutTextures.Clear();
        OutNormals.Clear();
        OutTriangles.Clear();
        ProcessVertices.Clear();
        ProcessTextures.Clear();
        ProcessNormals.Clear();
        ProcessBool.Clear();

        int trianglecount = 0;

        float[] planeDist = new float[3];

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

            ProcessVertices.Add(vertices[triangles[a]]);
            ProcessVertices.Add(vertices[triangles[a + 1]]);
            ProcessVertices.Add(vertices[triangles[a + 2]]);
            ProcessTextures.Add(textures[triangles[a]]);
            ProcessTextures.Add(textures[triangles[a + 1]]);
            ProcessTextures.Add(textures[triangles[a + 2]]);
            ProcessNormals.Add(normals[triangles[a]]);
            ProcessNormals.Add(normals[triangles[a + 1]]);
            ProcessNormals.Add(normals[triangles[a + 2]]);
            ProcessBool.Add(true);
            ProcessBool.Add(true);
            ProcessBool.Add(true);
        }

        for (int b = 0; b < planes.Length; b++)
        {
            int inIndex = 0;
            int outIndex1 = 0;
            int outIndex2 = 0;
            int outIndex = 0;
            int inIndex1 = 0;
            int inIndex2 = 0;

            int AddTriangles = 0;

            TemporaryVertices.Clear();

            TemporaryTextures.Clear();

            TemporaryNormals.Clear();

            for (int c = 0; c < ProcessVertices.Count; c += 3)
            {
                if (ProcessBool[c] == false && ProcessBool[c + 1] == false && ProcessBool[c + 2] == false)
                {
                    continue;
                }

                planeDist[0] = planes[b].GetDistanceToPoint(ProcessVertices[c]);
                planeDist[1] = planes[b].GetDistanceToPoint(ProcessVertices[c + 1]);
                planeDist[2] = planes[b].GetDistanceToPoint(ProcessVertices[c + 2]);
                bool b1 = planeDist[0] >= 0;
                bool b2 = planeDist[1] >= 0;
                bool b3 = planeDist[2] >= 0;

                int inCount = 0;

                if (b1)
                {
                    inCount += 1;
                }

                if (b2)
                {
                    inCount += 1;
                }

                if (b3)
                {
                    inCount += 1;
                }

                if (inCount == 3)
                {
                    continue;
                }
                else if (inCount == 1)
                {
                    if (b1 && !b2 && !b3)
                    {
                        inIndex = 0;
                        outIndex1 = 1;
                        outIndex2 = 2;
                    }
                    else if (!b1 && b2 && !b3)
                    {
                        outIndex1 = 2;
                        inIndex = 1;
                        outIndex2 = 0;
                    }
                    else if (!b1 && !b2 && b3)
                    {
                        outIndex1 = 0;
                        outIndex2 = 1;
                        inIndex = 2;
                    }

                    float t1 = planeDist[inIndex] / (planeDist[inIndex] - planeDist[outIndex1]);
                    float t2 = planeDist[inIndex] / (planeDist[inIndex] - planeDist[outIndex2]);

                    TemporaryVertices.Add(ProcessVertices[c + inIndex]);
                    TemporaryTextures.Add(ProcessTextures[c + inIndex]);
                    TemporaryNormals.Add(ProcessNormals[c + inIndex]);
                    TemporaryVertices.Add(Vector3.Lerp(ProcessVertices[c + inIndex], ProcessVertices[c + outIndex1], t1));
                    TemporaryTextures.Add(Vector2.Lerp(ProcessTextures[c + inIndex], ProcessTextures[c + outIndex1], t1));
                    TemporaryNormals.Add(Vector3.Lerp(ProcessNormals[c + inIndex], ProcessNormals[c + outIndex1], t1).normalized);
                    TemporaryVertices.Add(Vector3.Lerp(ProcessVertices[c + inIndex], ProcessVertices[c + outIndex2], t2));
                    TemporaryTextures.Add(Vector2.Lerp(ProcessTextures[c + inIndex], ProcessTextures[c + outIndex2], t2));
                    TemporaryNormals.Add(Vector3.Lerp(ProcessNormals[c + inIndex], ProcessNormals[c + outIndex2], t2).normalized);

                    ProcessBool[c] = false;
                    ProcessBool[c + 1] = false;
                    ProcessBool[c + 2] = false;

                    AddTriangles += 1;
                }
                else if (inCount == 2)
                {
                    if (!b1 && b2 && b3)
                    {
                        outIndex = 0;
                        inIndex1 = 1;
                        inIndex2 = 2;
                    }
                    else if (b1 && !b2 && b3)
                    {
                        inIndex1 = 2;
                        outIndex = 1;
                        inIndex2 = 0;
                    }
                    else if (b1 && b2 && !b3)
                    {
                        inIndex1 = 0;
                        inIndex2 = 1;
                        outIndex = 2;
                    }

                    float t1 = planeDist[inIndex1] / (planeDist[inIndex1] - planeDist[outIndex]);
                    float t2 = planeDist[inIndex2] / (planeDist[inIndex2] - planeDist[outIndex]);

                    TemporaryVertices.Add(ProcessVertices[c + inIndex1]);
                    TemporaryTextures.Add(ProcessTextures[c + inIndex1]);
                    TemporaryNormals.Add(ProcessNormals[c + inIndex1]);
                    TemporaryVertices.Add(ProcessVertices[c + inIndex2]);
                    TemporaryTextures.Add(ProcessTextures[c + inIndex2]);
                    TemporaryNormals.Add(ProcessNormals[c + inIndex2]);
                    TemporaryVertices.Add(Vector3.Lerp(ProcessVertices[c + inIndex1], ProcessVertices[c + outIndex], t1));
                    TemporaryTextures.Add(Vector2.Lerp(ProcessTextures[c + inIndex1], ProcessTextures[c + outIndex], t1));
                    TemporaryNormals.Add(Vector3.Lerp(ProcessNormals[c + inIndex1], ProcessNormals[c + outIndex], t1).normalized);
                    TemporaryVertices.Add(Vector3.Lerp(ProcessVertices[c + inIndex1], ProcessVertices[c + outIndex], t1));
                    TemporaryTextures.Add(Vector2.Lerp(ProcessTextures[c + inIndex1], ProcessTextures[c + outIndex], t1));
                    TemporaryNormals.Add(Vector3.Lerp(ProcessNormals[c + inIndex1], ProcessNormals[c + outIndex], t1).normalized);
                    TemporaryVertices.Add(ProcessVertices[c + inIndex2]);
                    TemporaryTextures.Add(ProcessTextures[c + inIndex2]);
                    TemporaryNormals.Add(ProcessNormals[c + inIndex2]);
                    TemporaryVertices.Add(Vector3.Lerp(ProcessVertices[c + inIndex2], ProcessVertices[c + outIndex], t2));
                    TemporaryTextures.Add(Vector2.Lerp(ProcessTextures[c + inIndex2], ProcessTextures[c + outIndex], t2));
                    TemporaryNormals.Add(Vector3.Lerp(ProcessNormals[c + inIndex2], ProcessNormals[c + outIndex], t2).normalized);

                    ProcessBool[c] = false;
                    ProcessBool[c + 1] = false;
                    ProcessBool[c + 2] = false;

                    AddTriangles += 1;
                }
                else if (inCount == 0)
                {
                    ProcessBool[c] = false;
                    ProcessBool[c + 1] = false;
                    ProcessBool[c + 2] = false;
                }
            }

            if (AddTriangles > 0)
            {
                for (int d = 0; d < TemporaryVertices.Count; d += 3)
                {
                    ProcessVertices.Add(TemporaryVertices[d]);
                    ProcessVertices.Add(TemporaryVertices[d + 1]);
                    ProcessVertices.Add(TemporaryVertices[d + 2]);
                    ProcessTextures.Add(TemporaryTextures[d]);
                    ProcessTextures.Add(TemporaryTextures[d + 1]);
                    ProcessTextures.Add(TemporaryTextures[d + 2]);
                    ProcessNormals.Add(TemporaryNormals[d]);
                    ProcessNormals.Add(TemporaryNormals[d + 1]);
                    ProcessNormals.Add(TemporaryNormals[d + 2]);
                    ProcessBool.Add(true);
                    ProcessBool.Add(true);
                    ProcessBool.Add(true);
                }
            }
        }

        for (int e = 0; e < ProcessBool.Count; e += 3)
        {
            if (ProcessBool[e] && ProcessBool[e + 1] && ProcessBool[e + 2])
            {
                OutVertices.Add(ProcessVertices[e]);
                OutVertices.Add(ProcessVertices[e + 1]);
                OutVertices.Add(ProcessVertices[e + 2]);
                OutTextures.Add(ProcessTextures[e]);
                OutTextures.Add(ProcessTextures[e + 1]);
                OutTextures.Add(ProcessTextures[e + 2]);
                OutNormals.Add(ProcessNormals[e]);
                OutNormals.Add(ProcessNormals[e + 1]);
                OutNormals.Add(ProcessNormals[e + 2]);
                OutTriangles.Add(trianglecount);
                OutTriangles.Add(trianglecount + 1);
                OutTriangles.Add(trianglecount + 2);
                trianglecount += 3;
            }
        }

        return (OutVertices, OutTextures, OutNormals, OutTriangles);
    }
}
