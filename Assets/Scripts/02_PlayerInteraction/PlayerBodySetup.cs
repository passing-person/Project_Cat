using UnityEngine;

/// <summary>
/// PlayerCat 根物体：脚本 + Rigidbody + CapsuleCollider。
/// 子物体 Model：仅模型（MeshFilter + MeshRenderer + PlayerModel）。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
[ExecuteAlways]
[DefaultExecutionOrder(-200)]
public class PlayerBodySetup : MonoBehaviour
{
    public const string ModelChildName = "Model";

    [Header("Model")]
    public float modelScale = 0.35f;

    [Header("Ground Check")]
    public float groundCheckYOffset = 0.02f;

    public Transform ModelTransform => transform.Find(ModelChildName);

    private void Awake()
    {
        Apply();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            Apply();
    }

    [ContextMenu("Apply Player Body Setup")]
    public void Apply()
    {
        RemoveSphereColliders();
        RemoveRootRenderers();
        RemoveRootCollidersExceptCapsule();

        Transform model = EnsureModelChild();
        SanitizeModelBranch(model);
        FitBodyCollider(model);
        PositionGroundCheck(model);
    }

    private void RemoveSphereColliders()
    {
        SphereCollider[] spheres = GetComponents<SphereCollider>();
        for (int i = 0; i < spheres.Length; i++)
            DestroySafe(spheres[i]);
    }

    private void RemoveRootCollidersExceptCapsule()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] is CapsuleCollider)
                continue;

            DestroySafe(colliders[i]);
        }
    }

    private Transform EnsureModelChild()
    {
        Transform model = transform.Find(ModelChildName);
        if (model == null)
        {
            Transform legacyVisual = transform.Find("Visual");
            if (legacyVisual != null)
                legacyVisual.name = ModelChildName;

            model = transform.Find(ModelChildName);
        }

        if (model != null)
        {
            model.localScale = Vector3.one * modelScale;
            EnsureModelComponent(model.gameObject);
            return model;
        }

        Mesh mesh = null;
        Material material = null;
        MeshFilter rootFilter = GetComponent<MeshFilter>();
        MeshRenderer rootRenderer = GetComponent<MeshRenderer>();
        if (rootFilter != null)
            mesh = rootFilter.sharedMesh;
        if (rootRenderer != null && rootRenderer.sharedMaterial != null)
            material = rootRenderer.sharedMaterial;

        if (mesh == null)
        {
            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            if (material == null)
                material = temp.GetComponent<MeshRenderer>().sharedMaterial;
            DestroySafe(temp);
        }

        GameObject modelObject = new GameObject(ModelChildName);
        modelObject.transform.SetParent(transform);
        modelObject.transform.localPosition = Vector3.zero;
        modelObject.transform.localRotation = Quaternion.identity;
        modelObject.transform.localScale = Vector3.one * modelScale;

        MeshFilter modelFilter = modelObject.AddComponent<MeshFilter>();
        MeshRenderer modelRenderer = modelObject.AddComponent<MeshRenderer>();
        modelFilter.sharedMesh = mesh;
        if (material != null)
            modelRenderer.sharedMaterial = material;

        EnsureModelComponent(modelObject);
        return modelObject.transform;
    }

    private static void EnsureModelComponent(GameObject modelObject)
    {
        if (modelObject.GetComponent<PlayerModel>() == null)
            modelObject.AddComponent<PlayerModel>();

        if (modelObject.GetComponent<MeshFilter>() == null)
            modelObject.AddComponent<MeshFilter>();

        if (modelObject.GetComponent<MeshRenderer>() == null)
            modelObject.AddComponent<MeshRenderer>();
    }

    private static void SanitizeModelBranch(Transform model)
    {
        if (model == null)
            return;

        Component[] components = model.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            if (component is Transform)
                continue;

            if (component is MeshFilter || component is MeshRenderer || component is PlayerModel || component is Animator)
                continue;

            DestroySafe(component);
        }

        Collider[] childColliders = model.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < childColliders.Length; i++)
            DestroySafe(childColliders[i]);

        Rigidbody[] rigidbodies = model.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < rigidbodies.Length; i++)
            DestroySafe(rigidbodies[i]);
    }

    private void FitBodyCollider(Transform model)
    {
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule == null)
            capsule = gameObject.AddComponent<CapsuleCollider>();

        Renderer renderer = model != null ? model.GetComponentInChildren<Renderer>() : null;
        if (renderer != null)
            ColliderFitUtility.FitCapsuleToRenderer(capsule, renderer);
    }

    private void PositionGroundCheck(Transform model)
    {
        Transform groundCheck = transform.Find("GroundCheck");
        if (groundCheck == null)
            return;

        Renderer renderer = model != null ? model.GetComponentInChildren<Renderer>() : null;
        if (renderer == null)
            return;

        Bounds bounds = renderer.bounds;
        Vector3 localFeet = transform.InverseTransformPoint(
            new Vector3(bounds.center.x, bounds.min.y + groundCheckYOffset, bounds.center.z));
        groundCheck.localPosition = localFeet;
    }

    private void RemoveRootRenderers()
    {
        MeshFilter filter = GetComponent<MeshFilter>();
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        if (filter != null)
            DestroySafe(filter);

        if (renderer != null)
            DestroySafe(renderer);
    }

    private static void DestroySafe(Object target)
    {
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target);
        else
            DestroyImmediate(target);
    }
}
