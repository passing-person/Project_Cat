using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.Events;

/// <summary>
/// Attach this to obstacles Security is allowed to destroy.
/// Keep these obstacles on the SecurityPathObstacleBreaker obstacleMask layer.
/// </summary>
public class SecurityBreakableObstacle : MonoBehaviour, ISecurityPathObstacle
{
    private enum BreakMode
    {
        DestroyRoot,
        DestroyThisGameObject,
        SetInactive,
        DisableCollidersAndNavObstacle,
        UnityEventOnly
    }

    [Header("Security Breakable")]
    [SerializeField] private bool securityCanBreak = true;
    [SerializeField] private BreakMode breakMode = BreakMode.DisableCollidersAndNavObstacle;
    [SerializeField, Min(0f)] private float breakDelay = 0f;

    [Tooltip("Optional explicit root to destroy when Break Mode is DestroyRoot. If null, uses this GameObject.")]
    [SerializeField] private GameObject rootToDestroy;

    [Header("Optional Explicit Components")]
    [SerializeField] private Collider[] collidersToDisable;
    [SerializeField] private NavMeshObstacle[] navMeshObstaclesToDisable;
    [SerializeField] private GameObject[] objectsToDisable;

    [Header("Events")]
    [SerializeField] private UnityEvent onSecurityBreak;

    private bool broken;

    public bool CanSecurityBreak(GameObject securityNpc)
    {
        return securityCanBreak && !broken;
    }

    public void BreakForSecurity(GameObject securityNpc)
    {
        if (!CanSecurityBreak(securityNpc))
            return;

        broken = true;

        if (breakDelay > 0f)
        {
            StartCoroutine(BreakAfterDelay());
            return;
        }

        ApplyBreak();
    }

    private IEnumerator BreakAfterDelay()
    {
        yield return new WaitForSeconds(breakDelay);
        ApplyBreak();
    }

    private void ApplyBreak()
    {
        onSecurityBreak?.Invoke();

        switch (breakMode)
        {
            case BreakMode.DestroyRoot:
                Destroy(rootToDestroy != null ? rootToDestroy : gameObject);
                break;

            case BreakMode.DestroyThisGameObject:
                Destroy(gameObject);
                break;

            case BreakMode.SetInactive:
                gameObject.SetActive(false);
                break;

            case BreakMode.DisableCollidersAndNavObstacle:
                DisableObstacleComponents();
                break;

            case BreakMode.UnityEventOnly:
                break;
        }
    }

    private void DisableObstacleComponents()
    {
        if (collidersToDisable == null || collidersToDisable.Length == 0)
            collidersToDisable = GetComponentsInChildren<Collider>();

        if (navMeshObstaclesToDisable == null || navMeshObstaclesToDisable.Length == 0)
            navMeshObstaclesToDisable = GetComponentsInChildren<NavMeshObstacle>();

        for (int i = 0; i < collidersToDisable.Length; i++)
        {
            if (collidersToDisable[i] != null)
                collidersToDisable[i].enabled = false;
        }

        for (int i = 0; i < navMeshObstaclesToDisable.Length; i++)
        {
            if (navMeshObstaclesToDisable[i] != null)
                navMeshObstaclesToDisable[i].enabled = false;
        }

        if (objectsToDisable != null)
        {
            for (int i = 0; i < objectsToDisable.Length; i++)
            {
                if (objectsToDisable[i] != null)
                    objectsToDisable[i].SetActive(false);
            }
        }
    }
}

public interface ISecurityPathObstacle
{
    bool CanSecurityBreak(GameObject securityNpc);
    void BreakForSecurity(GameObject securityNpc);
}
