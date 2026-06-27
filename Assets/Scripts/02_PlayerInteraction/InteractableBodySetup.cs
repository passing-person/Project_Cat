using UnityEngine;

/// <summary>
/// 可交互物体：模型在 Visual 子物体，InteractionZone 子物体放触发器（与模型分离）。
/// </summary>
[DisallowMultipleComponent]
[ExecuteAlways]
public class InteractableBodySetup : MonoBehaviour
{
    [Header("Interaction Zone")]
    public Vector3 zoneLocalCenter = new Vector3(0f, 10f, 0f);
    public Vector3 zoneSize = new Vector3(1.4f, 1.4f, 1.4f);

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            Apply();
    }

    [ContextMenu("Apply Interactable Body Setup")]
    public void Apply()
    {
        Transform visual = EnsureVisualChild();
        RemoveRootCollider();
        EnsureInteractionZone(visual);
    }

    private Transform EnsureVisualChild()
    {
        Transform visual = transform.Find("Visual");
        if (visual != null)
            return visual;

        MeshFilter rootFilter = GetComponent<MeshFilter>();
        MeshRenderer rootRenderer = GetComponent<MeshRenderer>();
        if (rootFilter == null || rootRenderer == null)
            return null;

        GameObject visualObject = new GameObject("Visual");
        visualObject.transform.SetParent(transform);
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localRotation = Quaternion.identity;
        visualObject.transform.localScale = Vector3.one;

        MeshFilter visualFilter = visualObject.AddComponent<MeshFilter>();
        MeshRenderer visualRenderer = visualObject.AddComponent<MeshRenderer>();
        visualFilter.sharedMesh = rootFilter.sharedMesh;
        visualRenderer.sharedMaterials = rootRenderer.sharedMaterials;

        if (Application.isPlaying)
        {
            Destroy(rootFilter);
            Destroy(rootRenderer);
        }
        else
        {
            DestroyImmediate(rootFilter);
            DestroyImmediate(rootRenderer);
        }

        return visualObject.transform;
    }

    private void RemoveRootCollider()
    {
        Collider rootCollider = GetComponent<Collider>();
        if (rootCollider == null)
            return;

        if (Application.isPlaying)
            Destroy(rootCollider);
        else
            DestroyImmediate(rootCollider);
    }

    private void EnsureInteractionZone(Transform visual)
    {
        Transform zone = transform.Find("InteractionZone");
        GameObject zoneObject;
        if (zone == null)
        {
            zoneObject = new GameObject("InteractionZone");
            zoneObject.transform.SetParent(transform);
        }
        else
        {
            zoneObject = zone.gameObject;
        }

        zoneObject.transform.localPosition = zoneLocalCenter;
        zoneObject.transform.localRotation = Quaternion.identity;

        Vector3 parentScale = transform.localScale;
        zoneObject.transform.localScale = new Vector3(
            1f / Mathf.Max(0.001f, parentScale.x),
            1f / Mathf.Max(0.001f, parentScale.y),
            1f / Mathf.Max(0.001f, parentScale.z));

        BoxCollider box = zoneObject.GetComponent<BoxCollider>();
        if (box == null)
            box = zoneObject.AddComponent<BoxCollider>();

        box.isTrigger = true;
        box.center = Vector3.zero;
        box.size = zoneSize;
    }
}
