using System.Collections.Generic;
using UnityEngine;

public class LineSegmentClipping : MonoBehaviour
{
    public List<List<Vector3>> ListsOfLists = new List<List<Vector3>>();

    public List<Vector3> segments = new List<Vector3>();

    public List<Vector3> worldsegments = new List<Vector3>();

    public List<Vector3> clippedsegments = new List<Vector3>();

    public List<Vector3> lines = new List<Vector3>();

    public Plane[] planes;

    public Vector3 ins1;

    public Vector3 ins2;

    bool t1;

    bool t2;

    public Camera Cam;

    public GameObject RenderQuad;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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

        for (int i = 0; i < 2; i++)
        {
            ListsOfLists.Add(new List<Vector3>());
        }
    }

    void Update()
    {
        planes = GeometryUtility.CalculateFrustumPlanes(Cam);

        lines.Clear();

        lines = PlaneClipLines(worldsegments, planes);

        if (lines.Count > 5)
        {
            RenderQuad.GetComponent<Renderer>().enabled = true;
        }
        else
        {
            RenderQuad.GetComponent<Renderer>().enabled = false;
        }
    }

    public List<Vector3> PlaneClipLines(List<Vector3> linesegments, Plane[] planes)
    {
        for (int i = 0; i < planes.Length; i++)
        {
            if (i % 2 == 0)
            {
                ListsOfLists[0].Clear();
                ListsOfLists[0].AddRange(linesegments);

                linesegments = ClipLines(ListsOfLists[0], planes[i]);
            }
            else
            {
                ListsOfLists[1].Clear();
                ListsOfLists[1].AddRange(linesegments);

                linesegments = ClipLines(ListsOfLists[1], planes[i]);
            }
        }

        return linesegments;
    }

    public List<Vector3> ClipLines(List<Vector3> linesegments, Plane plane)
    {
        clippedsegments.Clear();

        t1 = false;
        t2 = false;

        for (int i = 0; i < linesegments.Count; i += 2)
        {
            Vector3 v1 = linesegments[i];
            Vector3 v2 = linesegments[i + 1];
            float d1 = plane.GetDistanceToPoint(v1);
            float d2 = plane.GetDistanceToPoint(v2);

            if (d1 > 0 && d2 > 0) // Both v1 and v2 are in the plane
            {
                clippedsegments.Add(v1);
                clippedsegments.Add(v2);
            }
            else if (d1 > 0 && d2 < 0) // v1 is in and v2 is out of the plane
            {
                float t = d1 / (d1 - d2);

                ins1 = Vector3.Lerp(v1, v2, t);

                clippedsegments.Add(v1);
                clippedsegments.Add(ins1);

                t1 = true;
            }
            else if (d1 < 0 && d2 > 0) // v1 is out and v2 is in the plane
            {
                float t = d1 / (d1 - d2);

                ins2 = Vector3.Lerp(v1, v2, t);

                clippedsegments.Add(ins2);
                clippedsegments.Add(v2);

                t2 = true;
            }
        }
        if (t1 && t2)
        {
            clippedsegments.Add(ins1);
            clippedsegments.Add(ins2);
        }

        return clippedsegments;
    }
}
