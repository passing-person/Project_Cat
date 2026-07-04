using UnityEngine;

public static class ColliderFitUtility
{
    public static void FitCapsuleToRenderer(CapsuleCollider capsule, Renderer renderer)
    {
        if (capsule == null || renderer == null)
            return;

        Transform root = capsule.transform;
        Bounds bounds = renderer.bounds;

        float worldRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
        float worldHeight = Mathf.Max(bounds.size.y, worldRadius * 2f);

        capsule.direction = 1;
        capsule.center = root.InverseTransformPoint(bounds.center);
        capsule.radius = worldRadius;
        capsule.height = worldHeight;
        capsule.isTrigger = false;
    }

    public static void FitBoxToRenderer(BoxCollider box, Renderer renderer, Vector3 padding)
    {
        if (box == null || renderer == null)
            return;

        Transform root = box.transform;
        Bounds bounds = renderer.bounds;
        bounds.Expand(padding);

        box.center = root.InverseTransformPoint(bounds.center);
        box.size = root.InverseTransformVector(bounds.size);
        box.size = new Vector3(Mathf.Abs(box.size.x), Mathf.Abs(box.size.y), Mathf.Abs(box.size.z));
    }
}
