using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriangleClipping : MonoBehaviour
{
    public List<(List<Vector3>, List<Vector2>, List<Vector3>)> ListsOfTuples = new List<(List<Vector3>, List<Vector2>, List<Vector3>)>();

    public List<Vector3> OriginalVertices = new List<Vector3>();

    public List<Vector3> OriginalNormals = new List<Vector3>();

    public List<Vector2> OriginalTextures = new List<Vector2>();

    public List<Vector3> OriginalVerticesWorldTri = new List<Vector3>();

    public List<Vector2> OriginalTexturesTri = new List<Vector2>();

    public List<Vector3> OriginalVerticesTri = new List<Vector3>();

    public List<Vector3> OriginalNormalsTri = new List<Vector3>();

    public List<int> OriginalTriangles = new List<int>();

    public List<Vector3> OutVertices = new List<Vector3>();

    public List<Vector2> OutTextures = new List<Vector2>();

    public List<Vector3> OutNormals = new List<Vector3>();

    public List<int> OutTriangles = new List<int>();

    public Plane[] planes;

    public float[] d;

    public bool[] inside;

    public Camera Cam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inside = new bool[3];
        d = new float[3];

        for (int i = 0; i < 20; i++)
        {
            ListsOfTuples.Add((new List<Vector3>(), new List<Vector2>(), new List<Vector3>()));
        }

        planes = GeometryUtility.CalculateFrustumPlanes(Cam);

        Mesh mesh = GetComponent<MeshFilter>().mesh;

        mesh.GetVertices(OriginalVertices);
        mesh.GetUVs(0, OriginalTextures);
        mesh.GetTriangles(OriginalTriangles, 0);
        mesh.GetNormals(OriginalNormals);

        for (int i = 0; i < OriginalTriangles.Count; i += 3)
        {
            OriginalVerticesTri.Add(OriginalVertices[OriginalTriangles[i]]);
            OriginalVerticesTri.Add(OriginalVertices[OriginalTriangles[i + 1]]);
            OriginalVerticesTri.Add(OriginalVertices[OriginalTriangles[i + 2]]);
            OriginalTexturesTri.Add(OriginalTextures[OriginalTriangles[i]]);
            OriginalTexturesTri.Add(OriginalTextures[OriginalTriangles[i + 1]]);
            OriginalTexturesTri.Add(OriginalTextures[OriginalTriangles[i + 2]]);
            OriginalNormalsTri.Add(OriginalNormals[OriginalTriangles[i]]);
            OriginalNormalsTri.Add(OriginalNormals[OriginalTriangles[i + 1]]);
            OriginalNormalsTri.Add(OriginalNormals[OriginalTriangles[i + 2]]);
        }

        for (int i = 0; i < OriginalVerticesTri.Count; i++)
        {
            OriginalVerticesWorldTri.Add(transform.TransformPoint(OriginalVerticesTri[i]));
        }

        (List<Vector3>, List<Vector2>, List<Vector3>) outverttexnorm = ClipTrianglesVertTexNorm((OriginalVerticesWorldTri, OriginalTexturesTri, OriginalNormalsTri), planes);

        for (int i = 0; i < outverttexnorm.Item1.Count; i++)
        {
            OutTriangles.Add(i);
        }

        GameObject ClippedObject = new GameObject("Clipped");

        ClippedObject.AddComponent<MeshFilter>();
        ClippedObject.AddComponent<MeshRenderer>();

        Renderer ClippedRend = ClippedObject.GetComponent<Renderer>();
        ClippedRend.sharedMaterial = new Material(Shader.Find("Standard"));

        Mesh clippedmesh = new Mesh();

        clippedmesh.SetVertices(outverttexnorm.Item1);
        clippedmesh.SetUVs(0, outverttexnorm.Item2);
        clippedmesh.SetTriangles(OutTriangles, 0, true);
        clippedmesh.SetNormals(outverttexnorm.Item3);

        ClippedObject.GetComponent<MeshFilter>().mesh = clippedmesh;
    }

    public (List<Vector3>, List<Vector2>, List<Vector3>) ClipTriangles((List<Vector3>, List<Vector2>, List<Vector3>) verttexnorm, Plane plane)
    {
        OutVertices.Clear();
        OutTextures.Clear();
        OutNormals.Clear();

        for (int i = 0; i < verttexnorm.Item1.Count; i += 3)
        {
            int inCount = 0;

            d[0] = PointDistanceToPlane(plane, verttexnorm.Item1[i]);
            inside[0] = d[0] > 0;

            d[1] = PointDistanceToPlane(plane, verttexnorm.Item1[i + 1]);
            inside[1] = d[1] > 0;

            d[2] = PointDistanceToPlane(plane, verttexnorm.Item1[i + 2]);
            inside[2] = d[2] > 0;

            if (inside[0])
            {
                inCount++;
            }

            if (inside[1])
            {
                inCount++;
            }

            if (inside[2])
            {
                inCount++;
            }

            if (inCount == 3)
            {
                OutVertices.Add(verttexnorm.Item1[i]);
                OutVertices.Add(verttexnorm.Item1[i + 1]);
                OutVertices.Add(verttexnorm.Item1[i + 2]);
                OutTextures.Add(verttexnorm.Item2[i]);
                OutTextures.Add(verttexnorm.Item2[i + 1]);
                OutTextures.Add(verttexnorm.Item2[i + 2]);
                OutNormals.Add(verttexnorm.Item3[i]);
                OutNormals.Add(verttexnorm.Item3[i + 1]);
                OutNormals.Add(verttexnorm.Item3[i + 2]);
            }
            else if (inCount == 1)
            {
                int inIndex = inside[0] ? 0 : (inside[1] ? 1 : 2);
                int outIndex1 = (inIndex + 1) % 3;
                int outIndex2 = (inIndex + 2) % 3;
                float t1 = d[inIndex] / (d[inIndex] - d[outIndex1]);
                float t2 = d[inIndex] / (d[inIndex] - d[outIndex2]);
                OutVertices.Add(verttexnorm.Item1[i + inIndex]);
                OutTextures.Add(verttexnorm.Item2[i + inIndex]);
                OutNormals.Add(verttexnorm.Item3[i + inIndex]);
                OutVertices.Add(Interpolate(verttexnorm.Item1[i + inIndex], verttexnorm.Item1[i + outIndex1], t1));
                OutTextures.Add(Interpolate2D(verttexnorm.Item2[i + inIndex], verttexnorm.Item2[i + outIndex1], t1));
                OutNormals.Add(Interpolate(verttexnorm.Item3[i + inIndex], verttexnorm.Item3[i + outIndex1], t1).normalized);
                OutVertices.Add(Interpolate(verttexnorm.Item1[i + inIndex], verttexnorm.Item1[i + outIndex2], t2));
                OutTextures.Add(Interpolate2D(verttexnorm.Item2[i + inIndex], verttexnorm.Item2[i + outIndex2], t2));
                OutNormals.Add(Interpolate(verttexnorm.Item3[i + inIndex], verttexnorm.Item3[i + outIndex2], t2).normalized);
            }
            else if (inCount == 2)
            {
                int outIndex = inside[0] ? (inside[1] ? 2 : 1) : 0;
                int inIndex1 = (outIndex + 1) % 3;
                int inIndex2 = (outIndex + 2) % 3;
                float t1 = d[inIndex1] / (d[inIndex1] - d[outIndex]);
                float t2 = d[inIndex2] / (d[inIndex2] - d[outIndex]);
                OutVertices.Add(verttexnorm.Item1[i + inIndex1]);
                OutTextures.Add(verttexnorm.Item2[i + inIndex1]);
                OutNormals.Add(verttexnorm.Item3[i + inIndex1]);
                OutVertices.Add(verttexnorm.Item1[i + inIndex2]);
                OutTextures.Add(verttexnorm.Item2[i + inIndex2]);
                OutNormals.Add(verttexnorm.Item3[i + inIndex2]);
                OutVertices.Add(Interpolate(verttexnorm.Item1[i + inIndex1], verttexnorm.Item1[i + outIndex], t1));
                OutTextures.Add(Interpolate2D(verttexnorm.Item2[i + inIndex1], verttexnorm.Item2[i + outIndex], t1));
                OutNormals.Add(Interpolate(verttexnorm.Item3[i + inIndex1], verttexnorm.Item3[i + outIndex], t1).normalized);
                OutVertices.Add(Interpolate(verttexnorm.Item1[i + inIndex1], verttexnorm.Item1[i + outIndex], t1));
                OutTextures.Add(Interpolate2D(verttexnorm.Item2[i + inIndex1], verttexnorm.Item2[i + outIndex], t1));
                OutNormals.Add(Interpolate(verttexnorm.Item3[i + inIndex1], verttexnorm.Item3[i + outIndex], t1).normalized);
                OutVertices.Add(verttexnorm.Item1[i + inIndex2]);
                OutTextures.Add(verttexnorm.Item2[i + inIndex2]);
                OutNormals.Add(verttexnorm.Item3[i + inIndex2]);
                OutVertices.Add(Interpolate(verttexnorm.Item1[i + inIndex2], verttexnorm.Item1[i + outIndex], t2));
                OutTextures.Add(Interpolate2D(verttexnorm.Item2[i + inIndex2], verttexnorm.Item2[i + outIndex], t2));
                OutNormals.Add(Interpolate(verttexnorm.Item3[i + inIndex2], verttexnorm.Item3[i + outIndex], t2).normalized);
            }
        }

        return (OutVertices, OutTextures, OutNormals);
    }

    public (List<Vector3>, List<Vector2>, List<Vector3>) ClipTrianglesVertTexNorm((List<Vector3>, List<Vector2>, List<Vector3>) verttexnorm, Plane[] planes)
    {
        for (int i = 0; i < planes.Length; i++)
        {
            ListsOfTuples[i].Item1.Clear();

            ListsOfTuples[i].Item1.AddRange(verttexnorm.Item1);

            ListsOfTuples[i].Item2.Clear();

            ListsOfTuples[i].Item2.AddRange(verttexnorm.Item2);

            ListsOfTuples[i].Item3.Clear();

            ListsOfTuples[i].Item3.AddRange(verttexnorm.Item3);

            verttexnorm = ClipTriangles((ListsOfTuples[i].Item1, ListsOfTuples[i].Item2, ListsOfTuples[i].Item3), planes[i]);
        }

        return verttexnorm;
    }

    Vector3 Interpolate(Vector3 p0, Vector3 p1, float t)
    {
        return p0 + t * (p1 - p0);
    }

    Vector2 Interpolate2D(Vector2 p0, Vector2 p1, float t)
    {
        return p0 + t * (p1 - p0);
    }

    public float PointDistanceToPlane(Plane plane, Vector3 point)
    {
        return plane.normal.x * point.x + plane.normal.y * point.y + plane.normal.z * point.z + plane.distance;
    }
}
