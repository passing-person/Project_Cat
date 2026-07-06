using UnityEngine;
using UnityEngine.AI;

public class AdaptiveBlockLogic : MonoBehaviour, IMischiefWorldEventTarget
{
    [Header("NavMesh points")]
    [SerializeField] Transform normalReactionPoint;
    [SerializeField] Transform blockedFallbackPointA;
    [SerializeField] Transform blockedFallbackPointB;

    [Header("Shapes")]
    [SerializeField] GameObject original;
    [SerializeField] GameObject blocked;
    // Toggle when effectively interacted with

    private CoreFacade facade;

    private bool locked;

    public string WorldEventTargetId { get; }
    public MischiefWorldEventType WorldEventType { get; }
    public bool CanStartWorldEvent() => !locked;
    public string GetUnavailableReason() => locked ? "Target GameObject is locked" : string.Empty;

    private void Awake()
    {
        facade = FindFirstObjectByType<CoreFacade>();
    }

    public void OnWorldEventStarted(MischiefWorldEventContext context)
    {

    }

    public void OnWorldEventCompleted(bool disableTarget, float cooldownDuration)
    {

    }

    private void ToggleState()
    {
        
    }

    public bool IsPathBlockingLandscapeActive { get; private set; }

    // keep this.
    public bool TryGetFallbackReactionPositions(out Vector3 first, out Vector3 second)
    {
        first = blockedFallbackPointA != null ? blockedFallbackPointA.position : transform.position;
        second = blockedFallbackPointB != null ? blockedFallbackPointB.position : first;

        return blockedFallbackPointA != null || blockedFallbackPointB != null;
    }

    public bool TryGetClosestFallbackPoint(Vector3 startPoint, out Vector3 closest)
    {
        closest = Vector3.zero;
        if (TryGetFallbackReactionPositions(out Vector3 first, out Vector3 second))
        {
            bool chooseFirst = GetPathDistance(startPoint, first) < GetPathDistance(startPoint, second);
            closest = chooseFirst ? first : second;
            return true;
        }
        return false;
    }

    public static float GetPathDistance(Vector3 fromPosition, Vector3 toPosition, int areaMask = NavMesh.AllAreas)
    {
        NavMeshPath path = new();

        if (NavMesh.CalculatePath(fromPosition, toPosition, areaMask, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            float distance = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return distance;
        }

        return -1f; // Path incomplete or invalid
    }
}