using System.Collections.Generic;
using UnityEngine;

public class TestItFour : MonoBehaviour
{
    public Camera Cam;
    private RenderParams rp;
    private Mesh clippedmesh;
    private Mesh originalmesh;
    private Material material;
    private Vector3 camPosition;
    private Plane[] planes;
    private Renderer rend;
    private CameraMoved camMoved;
    private int[] processbool;
    private Vector3[] processvertices;
    private Vector2[] processtextures;
    private Vector3[] processnormals;
    private Vector3[] temporaryvertices;
    private Vector2[] temporarytextures;
    private Vector3[] temporarynormals;
    public List<Vector3> OriginalVertices = new List<Vector3>();
    public List<Vector3> OriginalVerticesWorld = new List<Vector3>();
    public List<Vector2> OriginalTextures = new List<Vector2>();
    public List<Vector3> OriginalNormals = new List<Vector3>();
    public List<Vector3> OriginalNormalsWorld = new List<Vector3>();
    public List<int> OriginalTriangles = new List<int>();
    public List<Vector3> OutVertices = new List<Vector3>();
    public List<Vector2> OutTextures = new List<Vector2>();
    public List<Vector3> OutNormals = new List<Vector3>();
    public List<int> OutTriangles = new List<int>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        originalmesh = this.GetComponent<MeshFilter>().mesh;

        originalmesh.GetVertices(OriginalVertices);
        originalmesh.GetUVs(0, OriginalTextures);
        originalmesh.GetNormals(OriginalNormals);
        originalmesh.GetTriangles(OriginalTriangles, 0);

        processbool = new int[256];
        processvertices = new Vector3[256];
        processtextures = new Vector2[256];
        processnormals = new Vector3[256];
        temporaryvertices = new Vector3[256];
        temporarytextures = new Vector2[256];
        temporarynormals = new Vector3[256];

        clippedmesh = new Mesh();

        material = this.GetComponent<MeshRenderer>().sharedMaterial;

        rend = this.GetComponent<Renderer>();

        camMoved = Cam.GetComponent<CameraMoved>();

        rp = new RenderParams();
    }

    void Update()
    {
        camPosition = Cam.transform.position;

        planes = GeometryUtility.CalculateFrustumPlanes(Cam);

        if (GeometryUtility.TestPlanesAABB(planes, rend.bounds))
        {
            if (this.transform.hasChanged || camMoved.TransformChanged)
            {
                OriginalVerticesWorld.Clear();
                OriginalNormalsWorld.Clear();

                for (int i = 0; i < OriginalVertices.Count; i++)
                {
                    OriginalVerticesWorld.Add(this.transform.TransformPoint(OriginalVertices[i]));
                    OriginalNormalsWorld.Add(this.transform.TransformDirection(OriginalNormals[i]));
                }

                (List<Vector3>, List<Vector2>, List<Vector3>, List<int>) Clipped = ClipTrianglesWithPlanes(OriginalVerticesWorld, OriginalTextures, OriginalNormalsWorld, OriginalTriangles, planes, camPosition);

                clippedmesh.Clear();

                clippedmesh.SetVertices(Clipped.Item1);
                clippedmesh.SetUVs(0, Clipped.Item2);
                clippedmesh.SetTriangles(Clipped.Item4, 0, true);
                clippedmesh.SetNormals(Clipped.Item3);

                this.transform.hasChanged = false;
            }

            rp.material = material;

            Graphics.RenderMesh(rp, clippedmesh, 0, Matrix4x4.identity);
        }
    }

    public (List<Vector3>, List<Vector2>, List<Vector3>, List<int>) ClipTrianglesWithPlanes(List<Vector3> vertices, List<Vector2> textures, List<Vector3> normals, List<int> triangles, Plane[] planes, Vector3 CamPosition)
    {
        OutVertices.Clear();
        OutTextures.Clear();
        OutNormals.Clear();
        OutTriangles.Clear();

        int trianglecount = 0;

        float[] planeDist = new float[3];

        for (int a = 0; a < triangles.Count; a += 3)
        {
            int processverticescount = 0;
            int processtexturescount = 0;
            int processnormalscount = 0;
            int processboolcount = 0;
            
            Vector3 Edge1 = vertices[triangles[a + 1]] - vertices[triangles[a]];
            Vector3 Edge2 = vertices[triangles[a + 2]] - vertices[triangles[a]];
            Vector3 Normal = Vector3.Cross(Edge1, Edge2).normalized;
            Vector3 CamDirection = (CamPosition - vertices[triangles[a]]).normalized;
            float triangleDirection = Vector3.Dot(Normal, CamDirection);

            if (triangleDirection < 0)
            {
                continue;
            }

            processvertices[processverticescount] = vertices[triangles[a]];
            processvertices[processverticescount + 1] = vertices[triangles[a + 1]];
            processvertices[processverticescount + 2] = vertices[triangles[a + 2]];
            processverticescount += 3;
            processtextures[processtexturescount] = textures[triangles[a]];
            processtextures[processtexturescount + 1] = textures[triangles[a + 1]];
            processtextures[processtexturescount + 2] = textures[triangles[a + 2]];
            processtexturescount += 3;
            processnormals[processnormalscount] = normals[triangles[a]];
            processnormals[processnormalscount + 1] = normals[triangles[a + 1]];
            processnormals[processnormalscount + 2] = normals[triangles[a + 2]];
            processnormalscount += 3;
            processbool[processboolcount] = 0;
            processbool[processboolcount + 1] = 0;
            processbool[processboolcount + 2] = 0;
            processboolcount += 3;

            for (int b = 0; b < planes.Length; b++)
            {
                int inIndex = 0;
                int outIndex1 = 0;
                int outIndex2 = 0;
                int outIndex = 0;
                int inIndex1 = 0;
                int inIndex2 = 0;
                int AddTriangles = 0;
                int temporaryverticescount = 0;
                int temporarytexturescount = 0;
                int temporarynormalscount = 0;

                for (int c = 0; c < processverticescount; c += 3)
                {
                    if (processbool[c] == 1 && processbool[c + 1] == 1 && processbool[c + 2] == 1)
                    {
                        continue;
                    }

                    planeDist[0] = planes[b].GetDistanceToPoint(processvertices[c]);
                    planeDist[1] = planes[b].GetDistanceToPoint(processvertices[c + 1]);
                    planeDist[2] = planes[b].GetDistanceToPoint(processvertices[c + 2]);
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

                        temporaryvertices[temporaryverticescount] = processvertices[c + inIndex];
                        temporaryvertices[temporaryverticescount + 1] = Vector3.Lerp(processvertices[c + inIndex], processvertices[c + outIndex1], t1);
                        temporaryvertices[temporaryverticescount + 2] = Vector3.Lerp(processvertices[c + inIndex], processvertices[c + outIndex2], t2);
                        temporaryverticescount += 3;
                        temporarytextures[temporarytexturescount] = processtextures[c + inIndex];
                        temporarytextures[temporarytexturescount + 1] = Vector2.Lerp(processtextures[c + inIndex], processtextures[c + outIndex1], t1);
                        temporarytextures[temporarytexturescount + 2] = Vector2.Lerp(processtextures[c + inIndex], processtextures[c + outIndex2], t2);
                        temporarytexturescount += 3;
                        temporarynormals[temporarynormalscount] = processnormals[c + inIndex];
                        temporarynormals[temporarynormalscount + 1] = Vector3.Lerp(processnormals[c + inIndex], processnormals[c + outIndex1], t1).normalized;
                        temporarynormals[temporarynormalscount + 2] = Vector3.Lerp(processnormals[c + inIndex], processnormals[c + outIndex2], t2).normalized;
                        temporarynormalscount += 3;

                        processbool[c] = 1;
                        processbool[c + 1] = 1;
                        processbool[c + 2] = 1;

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

                        temporaryvertices[temporaryverticescount] = processvertices[c + inIndex1];
                        temporaryvertices[temporaryverticescount + 1] = processvertices[c + inIndex2];
                        temporaryvertices[temporaryverticescount + 2] = Vector3.Lerp(processvertices[c + inIndex1], processvertices[c + outIndex], t1);
                        temporaryvertices[temporaryverticescount + 3] = Vector3.Lerp(processvertices[c + inIndex1], processvertices[c + outIndex], t1);
                        temporaryvertices[temporaryverticescount + 4] = processvertices[c + inIndex2];
                        temporaryvertices[temporaryverticescount + 5] = Vector3.Lerp(processvertices[c + inIndex2], processvertices[c + outIndex], t2);
                        temporaryverticescount += 6;
                        temporarytextures[temporarytexturescount] = processtextures[c + inIndex1];
                        temporarytextures[temporarytexturescount + 1] = processtextures[c + inIndex2];
                        temporarytextures[temporarytexturescount + 2] = Vector2.Lerp(processtextures[c + inIndex1], processtextures[c + outIndex], t1);
                        temporarytextures[temporarytexturescount + 3] = Vector2.Lerp(processtextures[c + inIndex1], processtextures[c + outIndex], t1);
                        temporarytextures[temporarytexturescount + 4] = processtextures[c + inIndex2];
                        temporarytextures[temporarytexturescount + 5] = Vector2.Lerp(processtextures[c + inIndex2], processtextures[c + outIndex], t2);
                        temporarytexturescount += 6;
                        temporarynormals[temporarynormalscount] = processnormals[c + inIndex1];
                        temporarynormals[temporarynormalscount + 1] = processnormals[c + inIndex2];
                        temporarynormals[temporarynormalscount + 2] = Vector3.Lerp(processnormals[c + inIndex1], processnormals[c + outIndex], t1).normalized;
                        temporarynormals[temporarynormalscount + 3] = Vector3.Lerp(processnormals[c + inIndex1], processnormals[c + outIndex], t1).normalized;
                        temporarynormals[temporarynormalscount + 4] = processnormals[c + inIndex2];
                        temporarynormals[temporarynormalscount + 5] = Vector3.Lerp(processnormals[c + inIndex2], processnormals[c + outIndex], t2).normalized;
                        temporarynormalscount += 6;

                        processbool[c] = 1;
                        processbool[c + 1] = 1;
                        processbool[c + 2] = 1;

                        AddTriangles += 2;
                    }
                    else if (inCount == 0)
                    {
                        processbool[c] = 1;
                        processbool[c + 1] = 1;
                        processbool[c + 2] = 1;
                    }
                }

                if (AddTriangles > 0)
                {
                    for (int d = 0; d < temporaryverticescount; d += 3)
                    {
                        processvertices[processverticescount] = temporaryvertices[d];
                        processvertices[processverticescount + 1] = temporaryvertices[d + 1];
                        processvertices[processverticescount + 2] = temporaryvertices[d + 2];
                        processverticescount += 3;
                        processtextures[processtexturescount] = temporarytextures[d];
                        processtextures[processtexturescount + 1] = temporarytextures[d + 1];
                        processtextures[processtexturescount + 2] = temporarytextures[d + 2];
                        processtexturescount += 3;
                        processnormals[processnormalscount] = temporarynormals[d];
                        processnormals[processnormalscount + 1] = temporarynormals[d + 1];
                        processnormals[processnormalscount + 2] = temporarynormals[d + 2];
                        processnormalscount += 3;
                        processbool[processboolcount] = 0;
                        processbool[processboolcount + 1] = 0;
                        processbool[processboolcount + 2] = 0;
                        processboolcount += 3;
                    }
                }
            }

            for (int e = 0; e < processboolcount; e += 3)
            {
                if (processbool[e] == 0 && processbool[e + 1] == 0 && processbool[e + 2] == 0)
                {
                    OutVertices.Add(processvertices[e]);
                    OutVertices.Add(processvertices[e + 1]);
                    OutVertices.Add(processvertices[e + 2]);
                    OutTextures.Add(processtextures[e]);
                    OutTextures.Add(processtextures[e + 1]);
                    OutTextures.Add(processtextures[e + 2]);
                    OutNormals.Add(processnormals[e]);
                    OutNormals.Add(processnormals[e + 1]);
                    OutNormals.Add(processnormals[e + 2]);
                    OutTriangles.Add(trianglecount);
                    OutTriangles.Add(trianglecount + 1);
                    OutTriangles.Add(trianglecount + 2);
                    trianglecount += 3;
                }
            }
        }

        return (OutVertices, OutTextures, OutNormals, OutTriangles);
    }
}
