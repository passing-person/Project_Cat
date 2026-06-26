using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(CapsuleCollider))]
public class NpcViewRange : MonoBehaviour
{
    [HideInInspector] public bool playerInViewRange = false;
    [SerializeField] private bool showGizmos = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            playerInViewRange = true;
            Debug.Log("Yes");
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponentInParent<PlayerController>() != null)
        {
            playerInViewRange = false;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col == null) return;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.color = new Color(1f, 1f, 0f, 0.3f);

        Vector3 localCenter = col.center;
        float radius = col.radius;
        float halfHeight = Mathf.Max(col.height / 2f - radius, 0f);

        // Two hemispheres (wire spheres)
        Gizmos.DrawWireSphere(localCenter + Vector3.up * halfHeight, radius);
        Gizmos.DrawWireSphere(localCenter - Vector3.up * halfHeight, radius);

        // Vertical edges of the cylindrical part
        int segments = 16;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 top = localCenter + Vector3.up * halfHeight + dir * radius;
            Vector3 bottom = localCenter - Vector3.up * halfHeight + dir * radius;
            Gizmos.DrawLine(top, bottom);
        }

        // Equator circles (top and bottom edges)
        DrawCircle(localCenter + Vector3.up * halfHeight, Vector3.up, radius, segments);
        DrawCircle(localCenter - Vector3.up * halfHeight, Vector3.up, radius, segments);

        Gizmos.matrix = Matrix4x4.identity;
    }

    private void DrawCircle(Vector3 center, Vector3 normal, float radius, int segments = 24)
    {
        Quaternion rotation = Quaternion.LookRotation(normal);
        Vector3 prev = center + rotation * new Vector3(radius, 0, 0);

        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            Vector3 dir = rotation * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
            Vector3 current = center + dir;
            Gizmos.DrawLine(prev, current);
            prev = current;
        }
    }

}
