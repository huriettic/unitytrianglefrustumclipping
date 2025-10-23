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
    private bool[] processbool;
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

        processbool = new bool[256];
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
            processbool[processboolcount] = true;
            processbool[processboolcount + 1] = true;
            processbool[processboolcount + 2] = true;
            processboolcount += 3;

            for (int b = 0; b < planes.Length; b++)
            {
                int AddTriangles = 0;
                int temporaryverticescount = 0;
                int temporarytexturescount = 0;
                int temporarynormalscount = 0;

                for (int c = 0; c < processverticescount; c += 3)
                {
                    if (processbool[c] == false && processbool[c + 1] == false && processbool[c + 2] == false)
                        continue;

                    Vector3 v0 = processvertices[c];
                    Vector3 v1 = processvertices[c + 1];
                    Vector3 v2 = processvertices[c + 2];

                    Vector2 uv0 = processtextures[c];
                    Vector2 uv1 = processtextures[c + 1];
                    Vector2 uv2 = processtextures[c + 2];

                    Vector3 n0 = processnormals[c];
                    Vector3 n1 = processnormals[c + 1];
                    Vector3 n2 = processnormals[c + 2];

                    float d0 = planes[b].GetDistanceToPoint(v0);
                    float d1 = planes[b].GetDistanceToPoint(v1);
                    float d2 = planes[b].GetDistanceToPoint(v2);

                    bool b0 = d0 >= 0;
                    bool b1 = d1 >= 0;
                    bool b2 = d2 >= 0;

                    if (b0 && b1 && b2)
                    {
                        continue;
                    }
                    else if ((b0 && !b1 && !b2) || (!b0 && b1 && !b2) || (!b0 && !b1 && b2))
                    {
                        Vector3 inV, outV1, outV2;
                        Vector2 inUV, outUV1, outUV2;
                        Vector3 inN, outN1, outN2;
                        float inD, outD1, outD2;

                        if (b0)
                        {
                            inV = v0;
                            inUV = uv0;
                            inN = n0;
                            inD = d0;
                            outV1 = v1;
                            outUV1 = uv1;
                            outN1 = n1;
                            outD1 = d1;
                            outV2 = v2;
                            outUV2 = uv2;
                            outN2 = n2;
                            outD2 = d2;
                        }
                        else if (b1)
                        {
                            inV = v1;
                            inUV = uv1;
                            inN = n1;
                            inD = d1;
                            outV1 = v2;
                            outUV1 = uv2;
                            outN1 = n2;
                            outD1 = d2;
                            outV2 = v0;
                            outUV2 = uv0;
                            outN2 = n0;
                            outD2 = d0;
                        }
                        else
                        {
                            inV = v2;
                            inUV = uv2;
                            inN = n2;
                            inD = d2;
                            outV1 = v0;
                            outUV1 = uv0;
                            outN1 = n0;
                            outD1 = d0;
                            outV2 = v1;
                            outUV2 = uv1;
                            outN2 = n1;
                            outD2 = d1;
                        }

                        float t1 = inD / (inD - outD1);
                        float t2 = inD / (inD - outD2);

                        temporaryvertices[temporaryverticescount] = inV;
                        temporaryvertices[temporaryverticescount + 1] = Vector3.Lerp(inV, outV1, t1);
                        temporaryvertices[temporaryverticescount + 2] = Vector3.Lerp(inV, outV2, t2);
                        temporaryverticescount += 3;
                        temporarytextures[temporarytexturescount] = inUV;
                        temporarytextures[temporarytexturescount + 1] = Vector2.Lerp(inUV, outUV1, t1);
                        temporarytextures[temporarytexturescount + 2] = Vector2.Lerp(inUV, outUV2, t2);
                        temporarytexturescount += 3;
                        temporarynormals[temporarynormalscount] = inN;
                        temporarynormals[temporarynormalscount + 1] = Vector3.Lerp(inN, outN1, t1).normalized;
                        temporarynormals[temporarynormalscount + 2] = Vector3.Lerp(inN, outN2, t2).normalized;
                        temporarynormalscount += 3;
                        processbool[c] = false;
                        processbool[c + 1] = false;
                        processbool[c + 2] = false;

                        AddTriangles += 1;
                    }
                    else if ((!b0 && b1 && b2) || (b0 && !b1 && b2) || (b0 && b1 && !b2))
                    {
                        Vector3 inV1, inV2, outV;
                        Vector2 inUV1, inUV2, outUV;
                        Vector3 inN1, inN2, outN;
                        float inD1, inD2, outD;

                        if (!b0)
                        {
                            outV = v0;
                            outUV = uv0;
                            outN = n0;
                            outD = d0;
                            inV1 = v1;
                            inUV1 = uv1;
                            inN1 = n1;
                            inD1 = d1;
                            inV2 = v2;
                            inUV2 = uv2;
                            inN2 = n2;
                            inD2 = d2;
                        }
                        else if (!b1)
                        {
                            outV = v1;
                            outUV = uv1;
                            outN = n1;
                            outD = d1;
                            inV1 = v2;
                            inUV1 = uv2;
                            inN1 = n2;
                            inD1 = d2;
                            inV2 = v0;
                            inUV2 = uv0;
                            inN2 = n0;
                            inD2 = d0;
                        }
                        else
                        {
                            outV = v2;
                            outUV = uv2;
                            outN = n2;
                            outD = d2;
                            inV1 = v0;
                            inUV1 = uv0;
                            inN1 = n0;
                            inD1 = d0;
                            inV2 = v1;
                            inUV2 = uv1;
                            inN2 = n1;
                            inD2 = d1;
                        }

                        float t1 = inD1 / (inD1 - outD);
                        float t2 = inD2 / (inD2 - outD);

                        Vector3 vA = Vector3.Lerp(inV1, outV, t1);
                        Vector3 vB = Vector3.Lerp(inV2, outV, t2);

                        Vector2 uvA = Vector2.Lerp(inUV1, outUV, t1);
                        Vector2 uvB = Vector2.Lerp(inUV2, outUV, t2);

                        Vector3 nA = Vector3.Lerp(inN1, outN, t1).normalized;
                        Vector3 nB = Vector3.Lerp(inN2, outN, t2).normalized;

                        temporaryvertices[temporaryverticescount] = inV1;
                        temporaryvertices[temporaryverticescount + 1] = inV2;
                        temporaryvertices[temporaryverticescount + 2] = vA;
                        temporaryverticescount += 3;
                        temporarytextures[temporarytexturescount] = inUV1;
                        temporarytextures[temporarytexturescount + 1] = inUV2;
                        temporarytextures[temporarytexturescount + 2] = uvA;
                        temporarytexturescount += 3;
                        temporarynormals[temporarynormalscount] = inN1;
                        temporarynormals[temporarynormalscount + 1] = inN2;
                        temporarynormals[temporarynormalscount + 2] = nA;
                        temporarynormalscount += 3;
                        temporaryvertices[temporaryverticescount] = vA;
                        temporaryvertices[temporaryverticescount + 1] = inV2;
                        temporaryvertices[temporaryverticescount + 2] = vB;
                        temporaryverticescount += 3;
                        temporarytextures[temporarytexturescount] = uvA;
                        temporarytextures[temporarytexturescount + 1] = inUV2;
                        temporarytextures[temporarytexturescount + 2] = uvB;
                        temporarytexturescount += 3;
                        temporarynormals[temporarynormalscount] = nA;
                        temporarynormals[temporarynormalscount + 1] = inN2;
                        temporarynormals[temporarynormalscount + 2] = nB;
                        temporarynormalscount += 3;
                        processbool[c] = false;
                        processbool[c + 1] = false;
                        processbool[c + 2] = false;

                        AddTriangles += 2;
                    }
                    else
                    {
                        processbool[c] = false;
                        processbool[c + 1] = false;
                        processbool[c + 2] = false;
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
                        processbool[processboolcount] = true;
                        processbool[processboolcount + 1] = true;
                        processbool[processboolcount + 2] = true;
                        processboolcount += 3;
                    }
                }
            }

            for (int e = 0; e < processboolcount; e += 3)
            {
                if (processbool[e] == true && processbool[e + 1] == true && processbool[e + 2] == true)
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
