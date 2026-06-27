using UnityEngine;

public static class InteractableUtility
{
    public static IInteractable GetInteractable(Component component)
    {
        if (component == null)
            return null;

        if (component is IInteractable direct)
            return direct;

        if (component.TryGetComponent(out IInteractable onSameObject))
            return onSameObject;

        foreach (MonoBehaviour behaviour in component.GetComponents<MonoBehaviour>())
        {
            if (behaviour is IInteractable interactable)
                return interactable;
        }

        foreach (MonoBehaviour behaviour in component.GetComponentsInParent<MonoBehaviour>())
        {
            if (behaviour is IInteractable interactable)
                return interactable;
        }

        return null;
    }

    public static float GetDistanceToInteractable(Vector3 from, IInteractable target)
    {
        if (target is not MonoBehaviour behaviour)
            return float.MaxValue;

        Collider[] colliders = behaviour.GetComponentsInChildren<Collider>();
        if (colliders.Length > 0)
        {
            float best = float.MaxValue;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled)
                    continue;

                float distance = Vector3.Distance(from, collider.ClosestPoint(from));
                if (distance < best)
                    best = distance;
            }

            return best;
        }

        return Vector3.Distance(from, behaviour.transform.position);
    }
}
