using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class TestItTwo : MonoBehaviour
{
    public List<List<Vector3>> ListsOfVertices = new List<List<Vector3>>();

    public List<List<Vector2>> ListsOfTextures = new List<List<Vector2>>();

    public List<List<Vector3>> ListsOfNormals = new List<List<Vector3>>();

    public float[] planeDist;

    public bool[] inSide;

    public Plane[] planes;

    public Matrix4x4 matrix;

    public Mesh clippedmesh;

    public Material material;

    public RenderParams rp;

    public Mesh originalmesh;

    public Vector3 camPosition;

    public int h;

    public List<Vector3> OriginalVertices = new List<Vector3>();

    public List<Vector3> OriginalVerticesWorld = new List<Vector3>();

    public List<Vector2> OriginalTextures = new List<Vector2>();

    public List<Vector3> OriginalNormals = new List<Vector3>();

    public List<int> OriginalTriangles = new List<int>();

    public List<Vector3> ProcessedVertices = new List<Vector3>();

    public List<Vector2> ProcessedTextures = new List<Vector2>();

    public List<Vector3> ProcessedNormals = new List<Vector3>();

    public List<int> ProcessedIndices = new List<int>();

    public List<Vector3> OutVerticesLocal = new List<Vector3>();

    public List<Vector3> OutVertices = new List<Vector3>();

    public List<Vector2> OutTextures = new List<Vector2>();

    public List<Vector3> OutNormals = new List<Vector3>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        planeDist = new float[3];

        inSide = new bool[3];

        clippedmesh = new Mesh();

        material = this.GetComponent<MeshRenderer>().sharedMaterial;

        originalmesh = this.GetComponent<MeshFilter>().mesh;

        originalmesh.GetUVs(0, OriginalTextures);
        originalmesh.GetNormals(OriginalNormals);
        originalmesh.GetTriangles(OriginalTriangles, 0);

        for (int i = 0; i < OriginalVertices.Count; i++)
        {
            OriginalVerticesWorld.Add(this.transform.TransformPoint(OriginalVertices[i]));
        }
        for (int i = 0; i < 2; i++)
        {
            ListsOfVertices.Add(new List<Vector3>());
        }
        for (int i = 0; i < 2; i++)
        {
            ListsOfTextures.Add(new List<Vector2>());
        }
        for (int i = 0; i < 2; i++)
        {
            ListsOfNormals.Add(new List<Vector3>());
        }
    }

    // Update is called once per frame
    void Update()
    {
        camPosition = Camera.main.transform.position;

        planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

        if (GeometryUtility.TestPlanesAABB(planes, this.GetComponent<Renderer>().bounds))
        {
            OriginalVertices.Clear();
            OriginalVerticesWorld.Clear();
            OutVerticesLocal.Clear();

            originalmesh.GetVertices(OriginalVertices);

            for (int i = 0; i < OriginalVertices.Count; i++)
            {
                OriginalVerticesWorld.Add(this.transform.TransformPoint(OriginalVertices[i]));
            }

            TestFunction(OriginalVerticesWorld, OriginalTextures, OriginalNormals, OriginalTriangles, planes);

            for (int i = 0; i < ProcessedVertices.Count; i++)
            {
                OutVerticesLocal.Add(this.transform.InverseTransformPoint(ProcessedVertices[i]));
            }

            clippedmesh.Clear();

            clippedmesh.SetVertices(OutVerticesLocal);
            clippedmesh.SetUVs(0, ProcessedTextures);
            clippedmesh.SetNormals(ProcessedNormals);
            clippedmesh.SetTriangles(ProcessedIndices, 0, true);

            rp.material = material;

            matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.lossyScale);

            Graphics.RenderMesh(rp, clippedmesh, 0, matrix);
        }
    }

    public void TestFunction(List<Vector3> vertices, List<Vector2> textures, List<Vector3> normals, List<int> triangles, Plane[] planes)
    {
        ProcessedVertices.Clear();
        ProcessedTextures.Clear();
        ProcessedNormals.Clear();
        ProcessedIndices.Clear();

        for (int i = 0; i < triangles.Count; i += 3)
        {
            Vector3 Edge1 = vertices[triangles[i + 1]] - vertices[triangles[i]];
            Vector3 Edge2 = vertices[triangles[i + 2]] - vertices[triangles[i]];
            Vector3 Normal = Vector3.Cross(Edge1, Edge2).normalized;
            Vector3 CamDirection = (camPosition - vertices[triangles[i]]).normalized;
            float triangleDirection = Vector3.Dot(Normal, CamDirection);

            if (triangleDirection < 0)
            {
                continue;
            }

            ListsOfVertices[0].Clear();
            ListsOfTextures[0].Clear();
            ListsOfNormals[0].Clear();

            ListsOfVertices[0].Add(vertices[triangles[i]]);
            ListsOfVertices[0].Add(vertices[triangles[i + 1]]);
            ListsOfVertices[0].Add(vertices[triangles[i + 2]]);

            ListsOfTextures[0].Add(textures[triangles[i]]);
            ListsOfTextures[0].Add(textures[triangles[i + 1]]);
            ListsOfTextures[0].Add(textures[triangles[i + 2]]);

            ListsOfNormals[0].Add(normals[triangles[i]]);
            ListsOfNormals[0].Add(normals[triangles[i + 1]]);
            ListsOfNormals[0].Add(normals[triangles[i + 2]]);

            for (int j = 0; j < planes.Length; j++)
            {
                if (j % 2 == 0)
                {
                    h = 0;
                }
                else
                {
                    h = 1;
                }

                OutVertices.Clear();
                OutTextures.Clear();
                OutNormals.Clear();

                for (int k = 0; k < ListsOfVertices[h].Count; k += 3)
                {
                    planeDist[0] = planes[j].GetDistanceToPoint(ListsOfVertices[h][k]);
                    planeDist[1] = planes[j].GetDistanceToPoint(ListsOfVertices[h][k + 1]);
                    planeDist[2] = planes[j].GetDistanceToPoint(ListsOfVertices[h][k + 2]);
                    inSide[0] = planeDist[0] > 0;
                    inSide[1] = planeDist[1] > 0;
                    inSide[2] = planeDist[2] > 0;

                    int InCount = 0;

                    int InIndex = 0;
                    int OutIndex1 = 0;
                    int OutIndex2 = 0;

                    int OutIndex = 0;
                    int InIndex1 = 1;
                    int InIndex2 = 2;

                    if (inSide[0])
                    {
                        InCount++;
                    }

                    if (inSide[1])
                    {
                        InCount++;
                    }

                    if (inSide[2])
                    {
                        InCount++;
                    }

                    if (InCount == 3)
                    {
                        OutVertices.Add(ListsOfVertices[h][k]);
                        OutVertices.Add(ListsOfVertices[h][k + 1]);
                        OutVertices.Add(ListsOfVertices[h][k + 2]);

                        OutTextures.Add(ListsOfTextures[h][k]);
                        OutTextures.Add(ListsOfTextures[h][k + 1]);
                        OutTextures.Add(ListsOfTextures[h][k + 2]);

                        OutNormals.Add(ListsOfNormals[h][k]);
                        OutNormals.Add(ListsOfNormals[h][k + 1]);
                        OutNormals.Add(ListsOfNormals[h][k + 2]);
                    }
                    else if (InCount == 1)
                    {
                        if (inSide[0] && !inSide[1] && !inSide[2])
                        {
                            InIndex = 0;
                            OutIndex1 = 1;
                            OutIndex2 = 2;
                        }
                        else if (!inSide[0] && inSide[1] && !inSide[2])
                        {
                            OutIndex1 = 2;
                            InIndex = 1;
                            OutIndex2 = 0;
                        }
                        else if (!inSide[0] && !inSide[1] && inSide[2])
                        {
                            OutIndex1 = 0;
                            OutIndex2 = 1;
                            InIndex = 2;
                        }

                        float d1 = planeDist[InIndex] / (planeDist[InIndex] - planeDist[OutIndex1]);
                        float d2 = planeDist[InIndex] / (planeDist[InIndex] - planeDist[OutIndex2]);

                        OutVertices.Add(ListsOfVertices[h][k + InIndex]);
                        OutTextures.Add(ListsOfTextures[h][k + InIndex]);
                        OutNormals.Add(ListsOfNormals[h][k + InIndex]);

                        OutVertices.Add(Vector3.Lerp(ListsOfVertices[h][k + InIndex], ListsOfVertices[h][k + OutIndex1], d1));
                        OutTextures.Add(Vector2.Lerp(ListsOfTextures[h][k + InIndex], ListsOfTextures[h][k + OutIndex1], d1));
                        OutNormals.Add(Vector3.Lerp(ListsOfNormals[h][k + InIndex], ListsOfNormals[h][k + OutIndex1], d1).normalized);

                        OutVertices.Add(Vector3.Lerp(ListsOfVertices[h][k + InIndex], ListsOfVertices[h][k + OutIndex2], d2));
                        OutTextures.Add(Vector2.Lerp(ListsOfTextures[h][k + InIndex], ListsOfTextures[h][k + OutIndex2], d2));
                        OutNormals.Add(Vector3.Lerp(ListsOfNormals[h][k + InIndex], ListsOfNormals[h][k + OutIndex2], d2).normalized);
                    }
                    else if (InCount == 2)
                    {
                        if (!inSide[0] && inSide[1] && inSide[2])
                        {
                            OutIndex = 0;
                            InIndex1 = 1;
                            InIndex2 = 2;
                        }
                        else if (inSide[0] && !inSide[1] && inSide[2])
                        {
                            InIndex1 = 2;
                            OutIndex = 1;
                            InIndex2 = 0;
                        }
                        else if (inSide[0] && inSide[1] && !inSide[2])
                        {
                            InIndex1 = 0;
                            InIndex2 = 1;
                            OutIndex = 2;
                        }

                        float d1 = planeDist[InIndex1] / (planeDist[InIndex1] - planeDist[OutIndex]);
                        float d2 = planeDist[InIndex2] / (planeDist[InIndex2] - planeDist[OutIndex]);

                        OutVertices.Add(ListsOfVertices[h][k + InIndex1]);
                        OutTextures.Add(ListsOfTextures[h][k + InIndex1]);
                        OutNormals.Add(ListsOfNormals[h][k + InIndex1]);

                        OutVertices.Add(ListsOfVertices[h][k + InIndex2]);
                        OutTextures.Add(ListsOfTextures[h][k + InIndex2]);
                        OutNormals.Add(ListsOfNormals[h][k + InIndex2]);

                        OutVertices.Add(Vector3.Lerp(ListsOfVertices[h][k + InIndex1], ListsOfVertices[h][k + OutIndex], d1));
                        OutTextures.Add(Vector2.Lerp(ListsOfTextures[h][k + InIndex1], ListsOfTextures[h][k + OutIndex], d1));
                        OutNormals.Add(Vector3.Lerp(ListsOfNormals[h][k + InIndex1], ListsOfNormals[h][k + OutIndex], d1).normalized);

                        OutVertices.Add(Vector3.Lerp(ListsOfVertices[h][k + InIndex1], ListsOfVertices[h][k + OutIndex], d1));
                        OutTextures.Add(Vector2.Lerp(ListsOfTextures[h][k + InIndex1], ListsOfTextures[h][k + OutIndex], d1));
                        OutNormals.Add(Vector3.Lerp(ListsOfNormals[h][k + InIndex1], ListsOfNormals[h][k + OutIndex], d1).normalized);

                        OutVertices.Add(ListsOfVertices[h][k + InIndex2]);
                        OutTextures.Add(ListsOfTextures[h][k + InIndex2]);
                        OutNormals.Add(ListsOfNormals[h][k + InIndex2]);

                        OutVertices.Add(Vector3.Lerp(ListsOfVertices[h][k + InIndex2], ListsOfVertices[h][k + OutIndex], d2));
                        OutTextures.Add(Vector2.Lerp(ListsOfTextures[h][k + InIndex2], ListsOfTextures[h][k + OutIndex], d2));
                        OutNormals.Add(Vector3.Lerp(ListsOfNormals[h][k + InIndex2], ListsOfNormals[h][k + OutIndex], d2).normalized);
                    }
                }

                if (j % 2 == 0)
                {
                    ListsOfVertices[1].Clear();
                    ListsOfTextures[1].Clear();
                    ListsOfNormals[1].Clear();

                    ListsOfVertices[1].AddRange(OutVertices);
                    ListsOfTextures[1].AddRange(OutTextures);
                    ListsOfNormals[1].AddRange(OutNormals);
                }
                else
                {
                    ListsOfVertices[0].Clear();
                    ListsOfTextures[0].Clear();
                    ListsOfNormals[0].Clear();

                    ListsOfVertices[0].AddRange(OutVertices);
                    ListsOfTextures[0].AddRange(OutTextures);
                    ListsOfNormals[0].AddRange(OutNormals);
                }
            }

            ProcessedVertices.AddRange(ListsOfVertices[h]);
            ProcessedTextures.AddRange(ListsOfTextures[h]);
            ProcessedNormals.AddRange(ListsOfNormals[h]);
        }

        for (int e = 0; e < ProcessedVertices.Count; e++)
        {
            ProcessedIndices.Add(e);
        }
    }
}  