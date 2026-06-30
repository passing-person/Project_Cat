using System.Collections.Generic;
using UnityEngine;

public class PatrolPath : MonoBehaviour
{
    [SerializeField]
    private List<Vector3> waypoints = new();

    [SerializeField]
    private bool isClosed = false;

    public IReadOnlyList<Vector3> Waypoints => waypoints;
    public bool IsClosed => isClosed;

    public Vector3 GetWorldPoint(int index)
    {
        return transform.TransformPoint(waypoints[index]);
    }

    private void OnDrawGizmos()
    {
        if (waypoints.Count == 0)
            return;

        Gizmos.color = Color.red;

        // Draw all waypoints
        for (int i = 0; i < waypoints.Count; i++)
        {
            Vector3 p = transform.TransformPoint(waypoints[i]);
            Gizmos.DrawSphere(p, .15f);

            if (i > 0)
            {
                Vector3 prev = transform.TransformPoint(waypoints[i - 1]);
                Vector3 direction = p - prev;
                Gizmos.DrawRay(prev, direction);
            }
        }

        if (isClosed && waypoints.Count > 1)
        {
            Vector3 first = transform.TransformPoint(waypoints[0]);
            Vector3 last = transform.TransformPoint(waypoints[^1]);
            Vector3 direction = first - last;
            Gizmos.DrawRay(last, direction);
        }
    }

    public void Add(Vector3 vector)
    {
        waypoints.Add(vector);
    }

    public void Clear()
    {
        waypoints.Clear();
    }
}