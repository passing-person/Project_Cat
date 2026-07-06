using UnityEngine;

/// <summary>
/// Compatibility wrapper for old cleanup calls.
/// It now delegates to MainCanvasUiRepair and never disables MainCanvas or Systems.
/// </summary>
public sealed class LegacyUiCleanup : MonoBehaviour
{
    [SerializeField] private bool runOnAwake = false;

    private void Awake()
    {
        if (runOnAwake)
        {
            MainCanvasUiRepair.Repair(gameObject);
        }
    }

    public static void CleanupNow(GameObject primaryUiRoot)
    {
        MainCanvasUiRepair.Repair(primaryUiRoot);
    }
}
