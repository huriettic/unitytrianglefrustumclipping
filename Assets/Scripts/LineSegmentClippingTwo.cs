using System.Collections.Generic;
using UnityEngine;

public class LineSegmentClippingTwo : MonoBehaviour
{
    public Camera Cam;

    public List<Vector3> segments = new List<Vector3>();

    public List<Vector3> worldsegments = new List<Vector3>();

    public List<Vector3> lines = new List<Vector3>();

    public List<int> lineints = new List<int>();

    private List<bool> ProcessBool = new List<bool>();

    private List<Vector3> ProcessVertices = new List<Vector3>();

    private List<Vector3> TemporaryVertices = new List<Vector3>();

    private List<Vector3> OutVertices = new List<Vector3>();

    public Plane[] planes;

    private Mesh linemesh;

    private Material lineMaterial;

    private RenderParams rp;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        linemesh = new Mesh();

        lineMaterial = new Material(Shader.Find("Standard"));

        lineMaterial.color = Color.cyan;

        rp = new RenderParams();

        segments = new List<Vector3>()
        {
            new Vector3(-0.5f, -0.5f, 0), new Vector3(-0.5f, 0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0), new Vector3(0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0), new Vector3(0.5f, -0.5f, 0),
            new Vector3(0.5f, -0.5f, 0), new Vector3(-0.5f, -0.5f, 0)
        };

        for (int i = 0; i < segments.Count; i++)
        {
            worldsegments.Add(this.transform.TransformPoint(segments[i]));
        }
    }

    void Update()
    {
        planes = GeometryUtility.CalculateFrustumPlanes(Cam);

        lines.Clear();

        lines = ClipEdgesWithPlanes(worldsegments, planes);

        lineints.Clear();

        for (int i = 0; i < lines.Count; i++)
        {
            lineints.Add(i);
        }

        if (lines.Count > 5)
        {
            linemesh.Clear();

            linemesh.SetVertices(lines);

            linemesh.SetIndices(lineints, MeshTopology.Lines, 0);

            rp.material = lineMaterial;

            Graphics.RenderMesh(rp, linemesh, 0, Matrix4x4.identity);
        }
    }

    public List<Vector3> ClipEdgesWithPlanes(List<Vector3> linesegments, Plane[] planes)
    {
        OutVertices.Clear();
        ProcessBool.Clear();
        ProcessVertices.Clear();

        Vector3[] lineSegment = new Vector3[2];

        Vector3[] intersectionPoints = new Vector3[2];

        for (int a = 0; a < linesegments.Count; a += 2)
        {
            ProcessVertices.Add(linesegments[a]);
            ProcessVertices.Add(linesegments[a + 1]);
            ProcessBool.Add(true);
            ProcessBool.Add(true);
        }

        for (int b = 0; b < planes.Length; b++)
        {
            int intersection = 0;
            int inIndex = 0;
            int outIndex = 0;

            TemporaryVertices.Clear();

            for (int c = 0; c < ProcessVertices.Count; c += 2)
            {
                if (ProcessBool[c] == false && ProcessBool[c + 1] == false)
                {
                    continue;
                }

                float d1 = planes[b].GetDistanceToPoint(ProcessVertices[c]);
                float d2 = planes[b].GetDistanceToPoint(ProcessVertices[c + 1]);
                bool b1 = d1 >= 0;
                bool b2 = d2 >= 0;

                if (b1 && b2)
                {
                    continue;
                }
                else if ((b1 && !b2) || (!b1 && b2))
                {
                    if (b1 && !b2)
                    {
                        inIndex = 0;
                        outIndex = 1;
                    }
                    else if (!b1 && b2)
                    {
                        inIndex = 1;
                        outIndex = 0;
                    }

                    float t = d1 / (d1 - d2);

                    intersectionPoints[outIndex] = Vector3.Lerp(ProcessVertices[c], ProcessVertices[c + 1], t);

                    lineSegment[inIndex] = ProcessVertices[c + inIndex];
                    lineSegment[outIndex] = intersectionPoints[outIndex];

                    TemporaryVertices.Add(lineSegment[0]);
                    TemporaryVertices.Add(lineSegment[1]);

                    ProcessBool[c] = false;
                    ProcessBool[c + 1] = false;

                    intersection += 1;
                }
                else
                {
                    ProcessBool[c] = false;
                    ProcessBool[c + 1] = false;
                }
            }

            if (intersection == 2)
            {
                for (int d = 0; d < TemporaryVertices.Count; d += 2)
                {
                    ProcessVertices.Add(TemporaryVertices[d]);
                    ProcessVertices.Add(TemporaryVertices[d + 1]);
                    ProcessBool.Add(true);
                    ProcessBool.Add(true);
                }

                ProcessVertices.Add(intersectionPoints[1]);
                ProcessVertices.Add(intersectionPoints[0]);

                ProcessBool.Add(true);
                ProcessBool.Add(true);
            }
        }

        for (int e = 0; e < ProcessBool.Count; e += 2)
        {
            if (ProcessBool[e] && ProcessBool[e + 1])
            {
                OutVertices.Add(ProcessVertices[e]);
                OutVertices.Add(ProcessVertices[e + 1]);
            }
        }

        return OutVertices;
    }
}
