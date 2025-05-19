using System.Collections.Generic;
using UnityEngine;

public class TriangleClipping : MonoBehaviour
{
    public List<(List<Vector3>, List<Vector2>, List<Vector3>, List<int>)> ListsOfLists = new List<(List<Vector3>, List<Vector2>, List<Vector3>, List<int>)>();

    public List<float> PlaneDistance = new List<float>();

    public List<bool> Inside = new List<bool>();

    public List<Vector3> OriginalVertices = new List<Vector3>();

    public List<Vector2> OriginalTextures = new List<Vector2>();

    public List<Vector3> OriginalNormals = new List<Vector3>();

    public List<int> OriginalTriangles = new List<int>();

    public List<Vector3> OriginalVerticesWorld = new List<Vector3>();

    public List<Vector2> OriginalTexturesTri = new List<Vector2>();

    public List<Vector3> OriginalVerticesTri = new List<Vector3>();

    public List<Vector3> OriginalNormalsTri = new List<Vector3>();

    public List<int> ProcessedTriangles = new List<int>();

    public List<Vector3> OutVertices = new List<Vector3>();

    public List<Vector2> OutTextures = new List<Vector2>();

    public List<Vector3> OutNormals = new List<Vector3>();

    public List<int> OutTriangles = new List<int>();

    public List<Vector3> OutVerticesLocal = new List<Vector3>();

    public List<Vector3> TriangleNormals = new List<Vector3>();

    public List<Vector3> CameraDirection = new List<Vector3>();

    public Plane[] planes;

    public Camera Cam;

    public int inIndex;

    public int inIndex1;

    public int inIndex2;

    public int outIndex;

    public int outIndex1;

    public int outIndex2;

    public float t1;

    public float t2;

    public int inCount;

    public Mesh clippedmesh;

    public Mesh originalmesh;

    public Material material;

    public RenderParams rp;

    public Vector3 camPosition;

    public Vector3 Edge1;

    public Vector3 Edge2;

    public Vector3 Normal;

    public Vector3 CamDirection;

    public float triangleDirection;

    public float triangleDistance1;

    public float triangleDistance2;

    public float triangleDistance3;

    public Matrix4x4 matrix;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        clippedmesh = new Mesh();

        originalmesh = this.GetComponent<MeshFilter>().mesh;

        originalmesh.GetUVs(0, OriginalTextures);
        originalmesh.GetNormals(OriginalNormals);
        originalmesh.GetTriangles(OriginalTriangles, 0);

        material = this.GetComponent<MeshRenderer>().sharedMaterial;

        for (int i = 0; i < 6; i++)
        {
            ListsOfLists.Add((new List<Vector3>(), new List<Vector2>(), new List<Vector3>(), new List<int>()));
        }
    }

    void Update()
    {
        camPosition = Cam.transform.position;

        planes = GeometryUtility.CalculateFrustumPlanes(Cam);

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

            (List<Vector3>, List<Vector2>, List<Vector3>, List<int>) convertingtriangles = ConvertToTriangles((OriginalVerticesWorld, OriginalTextures, OriginalNormals, OriginalTriangles));

            (List<Vector3>, List<Vector2>, List<Vector3>, List<int>) outverttexnormtri = ClipTrianglesVertTexNorm((convertingtriangles.Item1, convertingtriangles.Item2, convertingtriangles.Item3, convertingtriangles.Item4), planes);

            for (int i = 0; i < outverttexnormtri.Item1.Count; i++)
            {
                OutVerticesLocal.Add(this.transform.InverseTransformPoint(outverttexnormtri.Item1[i]));
            }

            clippedmesh.Clear();

            clippedmesh.SetVertices(OutVerticesLocal);
            clippedmesh.SetUVs(0, outverttexnormtri.Item2);
            clippedmesh.SetNormals(outverttexnormtri.Item3);
            clippedmesh.SetTriangles(outverttexnormtri.Item4, 0, true);
            
            rp.material = material;

            matrix = Matrix4x4.TRS(this.transform.position, this.transform.rotation, this.transform.lossyScale);

            Graphics.RenderMesh(rp, clippedmesh, 0, matrix);
        }
    }

    public (List<Vector3>, List<Vector2>, List<Vector3>, List<int>) ConvertToTriangles((List<Vector3>, List<Vector2>, List<Vector3>, List<int>) verttexnormtri)
    {
        OriginalVerticesTri.Clear();
        OriginalTexturesTri.Clear();
        OriginalNormalsTri.Clear();
        ProcessedTriangles.Clear();
        TriangleNormals.Clear();
        CameraDirection.Clear();

        for (int i = 0; i < verttexnormtri.Item4.Count; i += 3)
        {
            Edge1 = verttexnormtri.Item1[verttexnormtri.Item4[i + 1]] - verttexnormtri.Item1[verttexnormtri.Item4[i]];
            Edge2 = verttexnormtri.Item1[verttexnormtri.Item4[i + 2]] - verttexnormtri.Item1[verttexnormtri.Item4[i]];
            Normal = Vector3.Cross(Edge1, Edge2).normalized;
            CamDirection = (camPosition - verttexnormtri.Item1[verttexnormtri.Item4[i]]).normalized;
            triangleDirection = Vector3.Dot(Normal, CamDirection);

            if (triangleDirection < 0)
            {
                continue;
            }

            OriginalVerticesTri.Add(verttexnormtri.Item1[verttexnormtri.Item4[i]]);
            OriginalVerticesTri.Add(verttexnormtri.Item1[verttexnormtri.Item4[i + 1]]);
            OriginalVerticesTri.Add(verttexnormtri.Item1[verttexnormtri.Item4[i + 2]]);
            OriginalTexturesTri.Add(verttexnormtri.Item2[verttexnormtri.Item4[i]]);
            OriginalTexturesTri.Add(verttexnormtri.Item2[verttexnormtri.Item4[i + 1]]);
            OriginalTexturesTri.Add(verttexnormtri.Item2[verttexnormtri.Item4[i + 2]]);
            OriginalNormalsTri.Add(verttexnormtri.Item3[verttexnormtri.Item4[i]]);
            OriginalNormalsTri.Add(verttexnormtri.Item3[verttexnormtri.Item4[i + 1]]);
            OriginalNormalsTri.Add(verttexnormtri.Item3[verttexnormtri.Item4[i + 2]]);
            ProcessedTriangles.Add(i);
            ProcessedTriangles.Add(i + 1);
            ProcessedTriangles.Add(i + 2);
            TriangleNormals.Add(Normal);
            CameraDirection.Add(CamDirection);
        }

        return (OriginalVerticesTri, OriginalTexturesTri, OriginalNormalsTri, ProcessedTriangles);
    }

    public (List<Vector3>, List<Vector2>, List<Vector3>, List<int>) ClipTriangles((List<Vector3>, List<Vector2>, List<Vector3>, List<int>) verttexnormtri, Plane plane)
    {
        OutVertices.Clear();
        OutTextures.Clear();
        OutNormals.Clear();
        OutTriangles.Clear();
        PlaneDistance.Clear();
        Inside.Clear();

        for (int i = 0; i < verttexnormtri.Item1.Count; i += 3)
        {
            triangleDistance1 = plane.GetDistanceToPoint(verttexnormtri.Item1[i]);
            triangleDistance2 = plane.GetDistanceToPoint(verttexnormtri.Item1[i + 1]);
            triangleDistance3 = plane.GetDistanceToPoint(verttexnormtri.Item1[i + 2]);
            PlaneDistance.Add(triangleDistance1);
            PlaneDistance.Add(triangleDistance2);
            PlaneDistance.Add(triangleDistance3);
            Inside.Add(triangleDistance1 > 0);
            Inside.Add(triangleDistance2 > 0);
            Inside.Add(triangleDistance3 > 0);
        }
        for (int i = 0; i < verttexnormtri.Item1.Count; i += 3)
        {
            inCount = 0;

            if (Inside[i])
            {
                inCount++;
            }

            if (Inside[i + 1])
            {
                inCount++;
            }

            if (Inside[i + 2])
            {
                inCount++;
            }

            if (inCount == 3)
            {
                OutVertices.Add(verttexnormtri.Item1[i]);
                OutVertices.Add(verttexnormtri.Item1[i + 1]);
                OutVertices.Add(verttexnormtri.Item1[i + 2]);
                OutTextures.Add(verttexnormtri.Item2[i]);
                OutTextures.Add(verttexnormtri.Item2[i + 1]);
                OutTextures.Add(verttexnormtri.Item2[i + 2]);
                OutNormals.Add(verttexnormtri.Item3[i]);
                OutNormals.Add(verttexnormtri.Item3[i + 1]);
                OutNormals.Add(verttexnormtri.Item3[i + 2]);
                OutTriangles.Add(i);
                OutTriangles.Add(i + 1);
                OutTriangles.Add(i + 2);
            }
            else if (inCount == 1)
            {
                if (Inside[i] && !Inside[i + 1] && !Inside[i + 2])
                {
                    inIndex = 0;
                    outIndex1 = 1;
                    outIndex2 = 2;
                }
                else if (!Inside[i] && Inside[i + 1] && !Inside[i + 2])
                {
                    outIndex1 = 2;
                    inIndex = 1;
                    outIndex2 = 0;
                }
                else if (!Inside[i] && !Inside[i + 1] && Inside[i + 2])
                {
                    outIndex1 = 0;
                    outIndex2 = 1;
                    inIndex = 2;
                }

                t1 = PlaneDistance[i + inIndex] / (PlaneDistance[i + inIndex] - PlaneDistance[i + outIndex1]);
                t2 = PlaneDistance[i + inIndex] / (PlaneDistance[i + inIndex] - PlaneDistance[i + outIndex2]);

                OutVertices.Add(verttexnormtri.Item1[i + inIndex]);
                OutTextures.Add(verttexnormtri.Item2[i + inIndex]);
                OutNormals.Add(verttexnormtri.Item3[i + inIndex]);
                OutTriangles.Add(i + inIndex);
                OutVertices.Add(Vector3.Lerp(verttexnormtri.Item1[i + inIndex], verttexnormtri.Item1[i + outIndex1], t1));
                OutTextures.Add(Vector2.Lerp(verttexnormtri.Item2[i + inIndex], verttexnormtri.Item2[i + outIndex1], t1));
                OutNormals.Add(Vector3.Lerp(verttexnormtri.Item3[i + inIndex], verttexnormtri.Item3[i + outIndex1], t1).normalized);
                OutTriangles.Add(i + outIndex1);
                OutVertices.Add(Vector3.Lerp(verttexnormtri.Item1[i + inIndex], verttexnormtri.Item1[i + outIndex2], t2));
                OutTextures.Add(Vector2.Lerp(verttexnormtri.Item2[i + inIndex], verttexnormtri.Item2[i + outIndex2], t2));
                OutNormals.Add(Vector3.Lerp(verttexnormtri.Item3[i + inIndex], verttexnormtri.Item3[i + outIndex2], t2).normalized);
                OutTriangles.Add(i + outIndex2);
            }
            else if (inCount == 2)
            {
                if (!Inside[i] && Inside[i + 1] && Inside[i + 2])
                {
                    outIndex = 0;
                    inIndex1 = 1;
                    inIndex2 = 2;
                }
                else if (Inside[i] && !Inside[i + 1] && Inside[i + 2])
                {
                    inIndex1 = 2;
                    outIndex = 1;
                    inIndex2 = 0;
                }
                else if (Inside[i] && Inside[i + 1] && !Inside[i + 2])
                {
                    inIndex1 = 0;
                    inIndex2 = 1;
                    outIndex = 2;
                }

                t1 = PlaneDistance[i + inIndex1] / (PlaneDistance[i + inIndex1] - PlaneDistance[i + outIndex]);
                t2 = PlaneDistance[i + inIndex2] / (PlaneDistance[i + inIndex2] - PlaneDistance[i + outIndex]);

                OutVertices.Add(verttexnormtri.Item1[i + inIndex1]);
                OutTextures.Add(verttexnormtri.Item2[i + inIndex1]);
                OutNormals.Add(verttexnormtri.Item3[i + inIndex1]);
                OutTriangles.Add(i + inIndex1);
                OutVertices.Add(verttexnormtri.Item1[i + inIndex2]);
                OutTextures.Add(verttexnormtri.Item2[i + inIndex2]);
                OutNormals.Add(verttexnormtri.Item3[i + inIndex2]);
                OutTriangles.Add(i + inIndex2);
                OutVertices.Add(Vector3.Lerp(verttexnormtri.Item1[i + inIndex1], verttexnormtri.Item1[i + outIndex], t1));
                OutTextures.Add(Vector2.Lerp(verttexnormtri.Item2[i + inIndex1], verttexnormtri.Item2[i + outIndex], t1));
                OutNormals.Add(Vector3.Lerp(verttexnormtri.Item3[i + inIndex1], verttexnormtri.Item3[i + outIndex], t1).normalized);
                OutTriangles.Add(i + outIndex);
                OutVertices.Add(Vector3.Lerp(verttexnormtri.Item1[i + inIndex1], verttexnormtri.Item1[i + outIndex], t1));
                OutTextures.Add(Vector2.Lerp(verttexnormtri.Item2[i + inIndex1], verttexnormtri.Item2[i + outIndex], t1));
                OutNormals.Add(Vector3.Lerp(verttexnormtri.Item3[i + inIndex1], verttexnormtri.Item3[i + outIndex], t1).normalized);
                OutTriangles.Add(i + outIndex);
                OutVertices.Add(verttexnormtri.Item1[i + inIndex2]);
                OutTextures.Add(verttexnormtri.Item2[i + inIndex2]);
                OutNormals.Add(verttexnormtri.Item3[i + inIndex2]);
                OutTriangles.Add(i + inIndex2);
                OutVertices.Add(Vector3.Lerp(verttexnormtri.Item1[i + inIndex2], verttexnormtri.Item1[i + outIndex], t2));
                OutTextures.Add(Vector2.Lerp(verttexnormtri.Item2[i + inIndex2], verttexnormtri.Item2[i + outIndex], t2));
                OutNormals.Add(Vector3.Lerp(verttexnormtri.Item3[i + inIndex2], verttexnormtri.Item3[i + outIndex], t2).normalized);
                OutTriangles.Add(i + outIndex);
            }
        }

        return (OutVertices, OutTextures, OutNormals, OutTriangles);
    }

    public (List<Vector3>, List<Vector2>, List<Vector3>, List<int>) ClipTrianglesVertTexNorm((List<Vector3>, List<Vector2>, List<Vector3>, List<int>) verttexnormtri, Plane[] planes)
    {
        for (int i = 0; i < planes.Length; i++)
        {
            ListsOfLists[i].Item1.Clear();

            ListsOfLists[i].Item1.AddRange(verttexnormtri.Item1);

            ListsOfLists[i].Item2.Clear();

            ListsOfLists[i].Item2.AddRange(verttexnormtri.Item2);

            ListsOfLists[i].Item3.Clear();

            ListsOfLists[i].Item3.AddRange(verttexnormtri.Item3);

            ListsOfLists[i].Item4.Clear();

            ListsOfLists[i].Item4.AddRange(verttexnormtri.Item4);

            verttexnormtri = ClipTriangles((ListsOfLists[i].Item1, ListsOfLists[i].Item2, ListsOfLists[i].Item3, ListsOfLists[i].Item4), planes[i]);
        }

        return verttexnormtri;
    }
}
