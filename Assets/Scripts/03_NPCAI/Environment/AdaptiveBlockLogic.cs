using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AdaptiveBlockLogic : MonoBehaviour, IMischiefWorldEventTarget
{
    [Header("Core")]
    [SerializeField] private CoreFacade facade;
    [SerializeField] private string targetIdOverride = "";
    [SerializeField] private MischiefWorldEventType eventType = MischiefWorldEventType.Auto;

    [Header("NavMesh points")]
    [SerializeField] private Transform normalReactionPoint;
    [SerializeField] private Transform blockedFallbackPointA;
    [SerializeField] private Transform blockedFallbackPointB;

    [Header("Shapes")]
    [SerializeField] private GameObject original;
    [SerializeField] private GameObject blocked;
    [SerializeField] private bool initializeVisualStateOnStart = true;
    [SerializeField] private bool startBlockedOnWorldEvent = true;
    [SerializeField] private bool restoreOriginalOnComplete = true;

    private Coroutine unlockCoroutine;
    private bool locked;
    private bool registered;

    public string WorldEventTargetId => ResolveTargetId();
    public MischiefWorldEventType WorldEventType => ResolveWorldEventType();
    public bool IsPathBlockingLandscapeActive { get; private set; }

    public bool CanStartWorldEvent()
    {
        return !locked && !IsPathBlockingLandscapeActive;
    }

    public string GetUnavailableReason()
    {
        if (locked)
        {
            return "Target GameObject is locked";
        }

        if (IsPathBlockingLandscapeActive)
        {
            return "Target is already in active blocked state";
        }

        return string.Empty;
    }

    private void Awake()
    {
        ResolveFacade();
        if (initializeVisualStateOnStart)
        {
            SetBlockedState(false);
        }
    }

    private void OnEnable()
    {
        RegisterTargetIfPossible();
    }

    private void Start()
    {
        RegisterTargetIfPossible();
    }

    private void OnDisable()
    {
        if (facade != null && registered)
        {
            facade.UnregisterMischiefWorldEventTarget(this);
        }

        registered = false;
    }

    public void OnWorldEventStarted(MischiefWorldEventContext context)
    {
        if (!IsMatchingTarget(context.TargetId))
        {
            return;
        }

        locked = true;
        StopUnlockCoroutine();

        if (startBlockedOnWorldEvent)
        {
            SetBlockedState(true);
        }
    }

    public void OnWorldEventCompleted(bool disableTarget, float cooldownDuration)
    {
        if (restoreOriginalOnComplete)
        {
            SetBlockedState(false);
        }

        StopUnlockCoroutine();

        if (disableTarget)
        {
            locked = true;
            return;
        }

        if (cooldownDuration > 0f)
        {
            locked = true;
            unlockCoroutine = StartCoroutine(UnlockAfterCooldown(cooldownDuration));
            return;
        }

        locked = false;
    }

    private void ToggleState()
    {
        SetBlockedState(!IsPathBlockingLandscapeActive);
    }

    public bool TryGetNormalReactionPosition(out Vector3 position)
    {
        if (normalReactionPoint != null)
        {
            position = normalReactionPoint.position;
            return true;
        }

        position = transform.position;
        return false;
    }

    public bool TryGetCurrentReactionPosition(Vector3 startPoint, out Vector3 position)
    {
        if (IsPathBlockingLandscapeActive && TryGetClosestFallbackPoint(startPoint, out position))
        {
            return true;
        }

        if (normalReactionPoint != null)
        {
            position = normalReactionPoint.position;
            return true;
        }

        position = transform.position;
        return true;
    }

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
        if (!TryGetFallbackReactionPositions(out Vector3 first, out Vector3 second))
        {
            return false;
        }

        float firstDistance = GetPathDistance(startPoint, first);
        float secondDistance = GetPathDistance(startPoint, second);

        if (firstDistance < 0f && secondDistance < 0f)
        {
            closest = Vector3.Distance(startPoint, first) <= Vector3.Distance(startPoint, second) ? first : second;
            return true;
        }

        if (firstDistance < 0f)
        {
            closest = second;
            return true;
        }

        if (secondDistance < 0f)
        {
            closest = first;
            return true;
        }

        closest = firstDistance <= secondDistance ? first : second;
        return true;
    }

    public static float GetPathDistance(Vector3 fromPosition, Vector3 toPosition, int areaMask = NavMesh.AllAreas)
    {
        NavMeshPath path = new NavMeshPath();

        if (NavMesh.CalculatePath(fromPosition, toPosition, areaMask, path) && path.status == NavMeshPathStatus.PathComplete)
        {
            float distance = 0f;
            for (int i = 1; i < path.corners.Length; i++)
            {
                distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            }
            return distance;
        }

        return -1f;
    }

    private void SetBlockedState(bool active)
    {
        IsPathBlockingLandscapeActive = active;

        if (original != null)
        {
            original.SetActive(!active);
        }

        if (blocked != null)
        {
            blocked.SetActive(active);
        }
    }

    private void ResolveFacade()
    {
        if (facade == null)
        {
            facade = FindObjectOfType<CoreFacade>();
        }
    }

    private void RegisterTargetIfPossible()
    {
        ResolveFacade();
        if (facade == null || registered || string.IsNullOrWhiteSpace(WorldEventTargetId))
        {
            return;
        }

        facade.RegisterMischiefWorldEventTarget(this);
        registered = true;
    }

    private string ResolveTargetId()
    {
        if (!string.IsNullOrWhiteSpace(targetIdOverride))
        {
            return targetIdOverride;
        }

        IMischiefTarget mischiefTarget = GetComponent<IMischiefTarget>();
        if (mischiefTarget != null && !string.IsNullOrWhiteSpace(mischiefTarget.InteractionId))
        {
            return mischiefTarget.InteractionId;
        }

        return gameObject.name;
    }

    private MischiefWorldEventType ResolveWorldEventType()
    {
        if (eventType != MischiefWorldEventType.Auto)
        {
            return eventType;
        }

        IMischiefTarget mischiefTarget = GetComponent<IMischiefTarget>();
        MischiefType mischiefType = mischiefTarget != null ? mischiefTarget.MischiefType : MischiefType.Custom;
        return MischiefWorldEventContext.InferEventType(WorldEventTargetId, mischiefType);
    }

    private bool IsMatchingTarget(string targetId)
    {
        return string.IsNullOrWhiteSpace(targetId) || targetId == WorldEventTargetId;
    }

    private IEnumerator UnlockAfterCooldown(float cooldownDuration)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, cooldownDuration));
        locked = false;
        unlockCoroutine = null;
    }

    private void StopUnlockCoroutine()
    {
        if (unlockCoroutine != null)
        {
            StopCoroutine(unlockCoroutine);
            unlockCoroutine = null;
        }
    }
}
