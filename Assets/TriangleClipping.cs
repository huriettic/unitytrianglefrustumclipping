using System.Collections.Generic;
using UnityEngine;

public class TriangleClipping : MonoBehaviour
{
    public List<(List<Vector3>, List<Vector2>, List<Vector3>, List<int>)> ListsOfMeshes = new List<(List<Vector3>, List<Vector2>, List<Vector3>, List<int>)>();

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

        for (int i = 0; i < 6; i++)
        {
            ListsOfMeshes.Add((new List<Vector3>(), new List<Vector2>(), new List<Vector3>(), new List<int>()));
        }

        planes = GeometryUtility.CalculateFrustumPlanes(Cam);

        Mesh mesh = GetComponent<MeshFilter>().mesh;

        mesh.GetVertices(OriginalVertices);
        mesh.GetUVs(0, OriginalTextures);
        mesh.GetNormals(OriginalNormals);
        mesh.GetTriangles(OriginalTriangles, 0);

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

        (List<Vector3>, List<Vector2>, List<Vector3>, List<int>) outverttexnormtri = ClipTrianglesVertTexNorm((OriginalVerticesWorldTri, OriginalTexturesTri, OriginalNormalsTri, OriginalTriangles), planes);

        GameObject ClippedObject = new GameObject("Clipped");

        ClippedObject.AddComponent<MeshFilter>();
        ClippedObject.AddComponent<MeshRenderer>();

        Renderer ClippedRend = ClippedObject.GetComponent<Renderer>();
        ClippedRend.sharedMaterial = new Material(Shader.Find("Standard"));

        Mesh clippedmesh = new Mesh();

        clippedmesh.SetVertices(outverttexnormtri.Item1);
        clippedmesh.SetUVs(0, outverttexnormtri.Item2);
        clippedmesh.SetTriangles(outverttexnormtri.Item4, 0, true);
        clippedmesh.SetNormals(outverttexnormtri.Item3);

        ClippedObject.GetComponent<MeshFilter>().mesh = clippedmesh;
    }

    public (List<Vector3>, List<Vector2>, List<Vector3>, List<int>) ClipTriangles((List<Vector3>, List<Vector2>, List<Vector3>, List<int>) verttexnormtri, Plane plane)
    {
        OutVertices.Clear();
        OutTextures.Clear();
        OutNormals.Clear();
        OutTriangles.Clear();

        for (int i = 0; i < verttexnormtri.Item1.Count; i += 3)
        {
            int inCount = 0;
            
            d[0] = PointDistanceToPlane(plane, verttexnormtri.Item1[i]);
            inside[0] = d[0] > 0;

            d[1] = PointDistanceToPlane(plane, verttexnormtri.Item1[i + 1]);
            inside[1] = d[1] > 0;

            d[2] = PointDistanceToPlane(plane, verttexnormtri.Item1[i + 2]);
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
                int inIndex = inside[0] ? 0 : (inside[1] ? 1 : 2);
                int outIndex1 = (inIndex + 1) % 3;
                int outIndex2 = (inIndex + 2) % 3;
                float t1 = d[inIndex] / (d[inIndex] - d[outIndex1]);
                float t2 = d[inIndex] / (d[inIndex] - d[outIndex2]);
                OutVertices.Add(verttexnormtri.Item1[i + inIndex]);
                OutTextures.Add(verttexnormtri.Item2[i + inIndex]);
                OutNormals.Add(verttexnormtri.Item3[i + inIndex]);
                OutTriangles.Add(i + inIndex);
                OutVertices.Add(Interpolate(verttexnormtri.Item1[i + inIndex], verttexnormtri.Item1[i + outIndex1], t1));
                OutTextures.Add(Interpolate2D(verttexnormtri.Item2[i + inIndex], verttexnormtri.Item2[i + outIndex1], t1));
                OutNormals.Add(Interpolate(verttexnormtri.Item3[i + inIndex], verttexnormtri.Item3[i + outIndex1], t1).normalized);
                OutTriangles.Add(i + outIndex1);
                OutVertices.Add(Interpolate(verttexnormtri.Item1[i + inIndex], verttexnormtri.Item1[i + outIndex2], t2));
                OutTextures.Add(Interpolate2D(verttexnormtri.Item2[i + inIndex], verttexnormtri.Item2[i + outIndex2], t2));
                OutNormals.Add(Interpolate(verttexnormtri.Item3[i + inIndex], verttexnormtri.Item3[i + outIndex2], t2).normalized);
                OutTriangles.Add(i + outIndex2);
            }
            else if (inCount == 2)
            {
                int outIndex = inside[0] ? (inside[1] ? 2 : 1) : 0;
                int inIndex1 = (outIndex + 1) % 3;
                int inIndex2 = (outIndex + 2) % 3;
                float t1 = d[inIndex1] / (d[inIndex1] - d[outIndex]);
                float t2 = d[inIndex2] / (d[inIndex2] - d[outIndex]);
                OutVertices.Add(verttexnormtri.Item1[i + inIndex1]);
                OutTextures.Add(verttexnormtri.Item2[i + inIndex1]);
                OutNormals.Add(verttexnormtri.Item3[i + inIndex1]);
                OutTriangles.Add(i + inIndex1);
                OutVertices.Add(verttexnormtri.Item1[i + inIndex2]);
                OutTextures.Add(verttexnormtri.Item2[i + inIndex2]);
                OutNormals.Add(verttexnormtri.Item3[i + inIndex2]);
                OutTriangles.Add(i + inIndex2);
                OutVertices.Add(Interpolate(verttexnormtri.Item1[i + inIndex1], verttexnormtri.Item1[i + outIndex], t1));
                OutTextures.Add(Interpolate2D(verttexnormtri.Item2[i + inIndex1], verttexnormtri.Item2[i + outIndex], t1));
                OutNormals.Add(Interpolate(verttexnormtri.Item3[i + inIndex1], verttexnormtri.Item3[i + outIndex], t1).normalized);
                OutTriangles.Add(i + outIndex);
                OutVertices.Add(Interpolate(verttexnormtri.Item1[i + inIndex1], verttexnormtri.Item1[i + outIndex], t1));
                OutTextures.Add(Interpolate2D(verttexnormtri.Item2[i + inIndex1], verttexnormtri.Item2[i + outIndex], t1));
                OutNormals.Add(Interpolate(verttexnormtri.Item3[i + inIndex1], verttexnormtri.Item3[i + outIndex], t1).normalized);
                OutTriangles.Add(i + outIndex);
                OutVertices.Add(verttexnormtri.Item1[i + inIndex2]);
                OutTextures.Add(verttexnormtri.Item2[i + inIndex2]);
                OutNormals.Add(verttexnormtri.Item3[i + inIndex2]);
                OutTriangles.Add(i + inIndex2);
                OutVertices.Add(Interpolate(verttexnormtri.Item1[i + inIndex2], verttexnormtri.Item1[i + outIndex], t2));
                OutTextures.Add(Interpolate2D(verttexnormtri.Item2[i + inIndex2], verttexnormtri.Item2[i + outIndex], t2));
                OutNormals.Add(Interpolate(verttexnormtri.Item3[i + inIndex2], verttexnormtri.Item3[i + outIndex], t2).normalized);
                OutTriangles.Add(i + outIndex);
            }
        }

        return (OutVertices, OutTextures, OutNormals, OutTriangles);
    }

    public (List<Vector3>, List<Vector2>, List<Vector3>, List<int>) ClipTrianglesVertTexNorm((List<Vector3>, List<Vector2>, List<Vector3>, List<int>) verttexnormtri, Plane[] planes)
    {
        for (int i = 0; i < planes.Length; i++)
        {
            ListsOfMeshes[i].Item1.Clear();

            ListsOfMeshes[i].Item1.AddRange(verttexnormtri.Item1);

            ListsOfMeshes[i].Item2.Clear();

            ListsOfMeshes[i].Item2.AddRange(verttexnormtri.Item2);

            ListsOfMeshes[i].Item3.Clear();

            ListsOfMeshes[i].Item3.AddRange(verttexnormtri.Item3);

            ListsOfMeshes[i].Item4.Clear();

            ListsOfMeshes[i].Item4.AddRange(verttexnormtri.Item4);

            verttexnormtri = ClipTriangles((ListsOfMeshes[i].Item1, ListsOfMeshes[i].Item2, ListsOfMeshes[i].Item3, ListsOfMeshes[i].Item4), planes[i]);
        }

        return verttexnormtri;
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
