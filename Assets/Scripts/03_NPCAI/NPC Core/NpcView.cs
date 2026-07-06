using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;



public class NpcView : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] private NpcViewParams viewParams;

    [Header("Body parts")]
    [SerializeField] private GameObject head;

    [Header("Target Knowledge")]
    [Tooltip("If true, losing the player from the view sector stores the player's last actual position as the active snapshot instead of immediately clearing PlayerInView.")]
    [SerializeField] private bool snapshotWhenPlayerLeavesViewSector = true;

    [Tooltip("If true, when Search finishes and the player is still inside the larger view-range trigger, the NPC samples the player's current position as a snapshot target.")]
    [SerializeField] private bool allowSearchTimeoutSnapshotInsideViewRange = true;

    [Header("Dive Targeting")]
    [Tooltip("X = width, Y = depth. The player must stay inside this local-space rectangle for diveInterval before a normal dive can become valid.")]
    [FormerlySerializedAs("diveRectDimension")]
    [SerializeField] private Vector2 targetingRectDimension = new(.6f, 1.8f);

    [Tooltip("The player must continuously stay inside the targeting rect for this long before the space rect is evaluated.")]
    [SerializeField, Min(0f)] private float diveInterval = 0.2f;

    [Header("Dive Space Check")]
    [Tooltip("X = width, Y = depth. This local-space box is checked after the targeting interval to decide whether the normal dive has enough space.")]
    [SerializeField] private Vector2 ensureSpaceRectDimension = new(.6f, 1.8f);

    [Header("Close Range Dive")]
    [Tooltip("If true, NpcView uses NpcAnimationMachine.CloseRangeDiveDistanceThreshold as the source of truth when an animation machine exists on this NPC.")]
    [SerializeField] private bool useAnimationMachineCloseRangeDiveDistanceThreshold = true;

    [Tooltip("If the player is closer than this horizontal distance, PlayerInReach becomes true immediately. The animation machine should play this as a warp dive instead of a horizontal-shift dive.")]
    [FormerlySerializedAs("diveDistanceThreshold")]
    [SerializeField, Min(0f)] private float closeRangeDiveDistanceThreshold = 0.75f;

    [Header("View Direction Source")]
    [SerializeField] private bool useHeadFollowDirectionForViewSector = true;

    [Header("Reach")]
    [SerializeField, Min(0f)] private float catchRadius = 0.75f;

    [Tooltip("If true, dive range only counts while the player is inside the view range trigger.")]
    [SerializeField] private bool requireViewRangeForDive = true;

    [Tooltip("If true, catch range only counts while the player is inside the view range trigger.")]
    [SerializeField] private bool requireViewRangeForCatch = false;

    private Transform headTransform => head != null ? head.transform : transform;

    private NpcViewRange viewRange;
    private HeadFollowLogic headFollowLogic;
    private GameObject player;
    private CapsuleCollider capsule;
    private NpcAnimationMachine animationMachine;
    private NpcController controller;

    public event Action PlayerInViewFlagChange;
    public event Action PlayerActualViewFlagChange;
    public event Action PlayerInViewRangeFlagChange;
    public event Action PlayerInReachFlagChange;
    public event Action PlayerInCatchRangeFlagChange;
    public event Action KnownPlayerTargetChanged;

    /// <summary>
    /// True when the NPC has a usable target position: either actual sector visibility or an active snapshot.
    /// This is intentionally not the same as raw sector visibility.
    /// </summary>
    public bool PlayerInView => IsSecurity || _playerInView;

    /// <summary>
    /// True only while the player is inside the head-facing view sector and the larger view range.
    /// </summary>
    public bool PlayerInActualView => IsSecurity || _playerInActualView;

    public bool PlayerHidden
    {
        get
        {
            if (IsSecurity) return false;
            LazyInstantiate();
            return player.GetComponent<PlayerController>().IsHidden;
        }
    }
    public bool PlayerInViewRange => IsSecurity || _playerInViewRange;
    public bool HasPlayerPositionSnapshot => IsSecurity || _hasPlayerPositionSnapshot;
    public bool HasExitRangeSnapshot => IsSecurity || _hasExitRangeSnapshot;

    public bool PlayerInReach => _playerInDiveRange;
    public bool PlayerInCatchRange => _playerInCatchRange;

    public bool PlayerInTargetingRect => _playerInTargetingRect;
    public bool HasEnoughDiveSpace => _hasEnoughDiveSpace;
    public bool CloseRangeDiveRequested => _closeRangeDiveRequested;
    public bool DiveRequestIsValid => _playerInDiveRange && (_closeRangeDiveRequested || _hasEnoughDiveSpace);
    public float CloseRangeDiveDistanceThreshold => EffectiveCloseRangeDiveDistanceThreshold;

    public Vector3 PlayerPosition
    {
        get
        {
            return TryGetKnownPlayerPosition(out Vector3 position, out _)
                ? position
                : transform.position;
        }
    }

    public Vector3 ActualPlayerPosition
    {
        get
        {
            if (_hasActualPlayerPosition)
                return _actualPlayerPosition;

            return player != null ? player.transform.position : transform.position;
        }
    }

    public Vector3 PlayerSnapshotPosition => _playerPositionSnapshot;
    public Vector3 ExitRangeSnapshotPosition => _exitRangeSnapshotPosition;

    private bool _playerInView;
    private bool _playerInActualView;
    private bool _playerInViewRange;
    private bool _playerInDiveRange;
    private bool _playerInCatchRange;
    private bool _playerInTargetingRect;
    private bool _hasEnoughDiveSpace;
    private bool _closeRangeDiveRequested;

    private bool _hasActualPlayerPosition;
    private Vector3 _actualPlayerPosition;

    private bool _hasPlayerPositionSnapshot;
    private Vector3 _playerPositionSnapshot;

    private bool _hasExitRangeSnapshot;
    private Vector3 _exitRangeSnapshotPosition;

    private float diveReadyTimer;

    private float SectorRadius => viewParams != null ? viewParams.sectorRadius : 0f;
    private float SectorDeg => viewParams != null ? viewParams.sectorDeg : 0f;

    private float EffectiveCloseRangeDiveDistanceThreshold
    {
        get
        {
            if (useAnimationMachineCloseRangeDiveDistanceThreshold && animationMachine != null)
                return animationMachine.CloseRangeDiveDistanceThreshold;

            return closeRangeDiveDistanceThreshold;
        }
    }

    private bool IsSecurity
    {
        get
        {
            LazyInstantiate();
            return controller.IsSecurity;
        }
    }

    private void Awake()
    {
        LazyInstantiate();
    }

    private void Start()
    {
        LazyInstantiate();
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    public void Refresh()
    {
        LazyInstantiate();

        if (player == null || viewRange == null)
        {
            SetPlayerInViewRange(false);
            SetPlayerInActualView(false);
            _hasActualPlayerPosition = false;
            ClearActivePlayerSnapshot(false);
            RecalculatePlayerInView();
            ResetDiveReach();
            SetPlayerInCatchRange(false);
            return;
        }

        UpdatePlayerTargetKnowledge();
        UpdatePlayerDiveRange();
        UpdatePlayerCatchRange();
    }

    /// <summary>
    /// Returns the player's current transform position. This is a raw snapshot request,
    /// not necessarily the NPC's current target knowledge.
    /// </summary>
    public bool TryGetPlayerPositionSnapshot(out Vector3 position)
    {
        LazyInstantiate();

        if (player == null && PlayerHidden)
        {
            position = transform.position; // meaningless
            return false;
        }

        position = player.transform.position;
        return true;
    }

    public bool CapturePlayerPositionSnapshot()
    {
        if (!TryGetPlayerPositionSnapshot(out Vector3 position))
            return false;

        SetActivePlayerSnapshot(position);
        return true;
    }

    public bool TryGetKnownPlayerPosition(out Vector3 position)
    {
        return TryGetKnownPlayerPosition(out position, out _);
    }

    public bool TryGetKnownPlayerPosition(out Vector3 position, out NpcChaseTargetKind targetKind)
    {
        if (IsSecurity)
        {
            position = player.transform.position;
            targetKind = NpcChaseTargetKind.Actual;
            return true;
        }

        if (_playerInActualView && _hasActualPlayerPosition)
        {
            position = _actualPlayerPosition;
            targetKind = NpcChaseTargetKind.Actual;
            return true;
        }

        if (_hasPlayerPositionSnapshot)
        {
            position = _playerPositionSnapshot;
            targetKind = NpcChaseTargetKind.Snapshot;
            return true;
        }

        position = transform.position;
        targetKind = NpcChaseTargetKind.None;
        return false;
    }

    public bool TryGetActivePlayerSnapshot(out Vector3 position)
    {
        if (IsSecurity)
        {
            position = player.transform.position;
            return true;
        }

        position = _playerPositionSnapshot;
        return _hasPlayerPositionSnapshot;
    }

    public bool TryGetExitRangeSnapshot(out Vector3 position)
    {
        if (IsSecurity)
        {
            position = player.transform.position;
            return true;
        }

        position = _exitRangeSnapshotPosition;
        return _hasExitRangeSnapshot;
    }

    public void ClearActivePlayerSnapshot(bool notify = true)
    {
        if (!_hasPlayerPositionSnapshot)
        {
            if (notify)
                RecalculatePlayerInView();
            return;
        }

        _hasPlayerPositionSnapshot = false;

        if (notify)
        {
            KnownPlayerTargetChanged?.Invoke();
            RecalculatePlayerInView();
        }
    }

    public void ClearExitRangeSnapshot()
    {
        _hasExitRangeSnapshot = false;
    }

    public void ClearAllPlayerSnapshots()
    {
        bool hadAnySnapshot = _hasPlayerPositionSnapshot || _hasExitRangeSnapshot;

        _hasPlayerPositionSnapshot = false;
        _hasExitRangeSnapshot = false;

        if (hadAnySnapshot)
            KnownPlayerTargetChanged?.Invoke();

        RecalculatePlayerInView();
    }

    /// <summary>
    /// Search timeout rule:
    /// 1. If the player is actually visible in the sector, chase actual.
    /// 2. Else, if the player is still inside the larger view range, sample current position as a snapshot.
    /// 3. Else, reactivate the last exit-range snapshot.
    /// </summary>
    public bool TryPrepareSearchTimeoutChaseTarget(out Vector3 position, out NpcChaseTargetKind targetKind)
    {
        if (IsSecurity)
        {
            position = player.transform.position;
            targetKind = NpcChaseTargetKind.Actual;
            return true;
        }

        if (_playerInActualView && _hasActualPlayerPosition)
        {
            position = _actualPlayerPosition;
            targetKind = NpcChaseTargetKind.Actual;
            return true;
        }

        if (allowSearchTimeoutSnapshotInsideViewRange && _playerInViewRange && CapturePlayerPositionSnapshot())
        {
            position = _playerPositionSnapshot;
            targetKind = NpcChaseTargetKind.Snapshot;
            return true;
        }

        if (_hasExitRangeSnapshot)
        {
            SetActivePlayerSnapshot(_exitRangeSnapshotPosition);
            position = _playerPositionSnapshot;
            targetKind = NpcChaseTargetKind.Snapshot;
            return true;
        }

        position = transform.position;
        targetKind = NpcChaseTargetKind.None;
        return false;
    }

    public bool TryGetCloseRangeDiveWarpTarget(out Vector3 position)
    {
        position = transform.position;

        if (!_closeRangeDiveRequested)
            return false;

        return TryGetPlayerPositionSnapshot(out position);
    }

    private void UpdatePlayerTargetKnowledge()
    {
        bool wasInViewRange = _playerInViewRange;
        bool isInViewRange = viewRange != null && viewRange.playerInViewRange;
        SetPlayerInViewRange(isInViewRange);

        if (wasInViewRange && !isInViewRange)
            CaptureExitRangeSnapshot();

        bool wasInActualView = _playerInActualView;
        bool isInActualView = isInViewRange && IsPlayerInsideViewSector();

        if (isInActualView)
        {
            _actualPlayerPosition = player.transform.position;
            _hasActualPlayerPosition = true;
            ClearActivePlayerSnapshot(false);
            ClearExitRangeSnapshot();
        }
        else
        {
            _hasActualPlayerPosition = false;

            if (wasInActualView && snapshotWhenPlayerLeavesViewSector)
                CapturePlayerPositionSnapshot();
        }

        SetPlayerInActualView(isInActualView);
        RecalculatePlayerInView();
    }

    private bool IsPlayerInsideViewSector()
    {
        if (IsSecurity) return true;

        if (player == null || viewParams == null)
            return false;

        Vector3 toPlayer = player.transform.position - transform.position;
        Vector3 flatToPlayer = toPlayer;
        flatToPlayer.y = 0f;

        if (flatToPlayer.sqrMagnitude > SectorRadius * SectorRadius)
            return false;

        if (SectorDeg >= 360f)
            return true;


        if (useHeadFollowDirectionForViewSector &&
            headFollowLogic != null &&
            headFollowLogic.TryGetFlatViewDirection(out Vector3 flatForward))
        {
            // Use HeadFollow / IK gaze direction.
        }
        else
        {
            flatForward = headTransform.forward;
            flatForward.y = 0f;

            if (flatForward.sqrMagnitude <= 0.0001f)
                flatForward = transform.forward;

            flatForward.Normalize();
        }

        return Vector3.Angle(flatForward, flatToPlayer) < SectorDeg * 0.5f;
    }

    private void CaptureExitRangeSnapshot()
    {
        if (!TryGetPlayerPositionSnapshot(out Vector3 position))
            return;

        _exitRangeSnapshotPosition = position;
        _hasExitRangeSnapshot = true;
        SetActivePlayerSnapshot(position);
    }

    private void SetActivePlayerSnapshot(Vector3 position)
    {
        if (IsSecurity)
        {
            position = player.transform.position;
            return;
        }

        bool changed = !_hasPlayerPositionSnapshot ||
            (position - _playerPositionSnapshot).sqrMagnitude > 0.0001f;

        _playerPositionSnapshot = position;
        _hasPlayerPositionSnapshot = true;

        if (changed)
            KnownPlayerTargetChanged?.Invoke();

        RecalculatePlayerInView();
    }

    private void RecalculatePlayerInView()
    {
        SetPlayerInView(_playerInActualView || _hasPlayerPositionSnapshot);
    }

    private void UpdatePlayerDiveRange()
    {
        if (requireViewRangeForDive && !_playerInViewRange)
        {
            ResetDiveReach();
            return;
        }

        if (IsPlayerWithinHorizontalRadius(EffectiveCloseRangeDiveDistanceThreshold))
        {
            _playerInTargetingRect = false;
            _hasEnoughDiveSpace = false;
            _closeRangeDiveRequested = true;
            diveReadyTimer = 0f;
            SetPlayerInDiveRange(true);
            return;
        }

        _closeRangeDiveRequested = false;
        _playerInTargetingRect = IsPlayerInsideTargetingRect();

        if (!_playerInTargetingRect)
        {
            diveReadyTimer = 0f;
            _hasEnoughDiveSpace = false;
            SetPlayerInDiveRange(false);
            return;
        }

        diveReadyTimer += Time.deltaTime;

        if (diveReadyTimer < diveInterval)
        {
            _hasEnoughDiveSpace = false;
            SetPlayerInDiveRange(false);
            return;
        }

        _hasEnoughDiveSpace = EnoughSpaceToDive();
        SetPlayerInDiveRange(_hasEnoughDiveSpace);
    }

    private void UpdatePlayerCatchRange()
    {
        if (requireViewRangeForCatch && !_playerInViewRange)
        {
            SetPlayerInCatchRange(false);
            return;
        }

        SetPlayerInCatchRange(IsPlayerWithinHorizontalRadius(catchRadius));
    }

    private bool IsPlayerWithinHorizontalRadius(float radius)
    {
        if (player == null || radius <= 0f)
            return false;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;

        return toPlayer.sqrMagnitude <= radius * radius;
    }

    private bool IsPlayerInsideTargetingRect()
    {
        if (player == null)
            return false;

        if (!TryGetTargetingRect(out Vector3 center, out Quaternion rotation, out Vector3 halfExtents))
            return false;

        Vector3 localPlayerPosition = Quaternion.Inverse(rotation) * (player.transform.position - center);

        return Mathf.Abs(localPlayerPosition.x) <= halfExtents.x
            && Mathf.Abs(localPlayerPosition.z) <= halfExtents.z;
    }

    private void ResetDiveReach()
    {
        diveReadyTimer = 0f;
        _playerInTargetingRect = false;
        _hasEnoughDiveSpace = false;
        _closeRangeDiveRequested = false;
        SetPlayerInDiveRange(false);
    }

    private void SetPlayerInView(bool value)
    {
        if (IsSecurity)
        {
            _playerInView = true;
            return;
        }
        
        if (_playerInView == value)
            return;

        _playerInView = value;
        PlayerInViewFlagChange?.Invoke();
    }

    private void SetPlayerInActualView(bool value)
    {
        if (IsSecurity)
        {
            _playerInActualView = true;
            return;
        }

        if (_playerInActualView == value)
            return;

        _playerInActualView = value;
        PlayerActualViewFlagChange?.Invoke();
    }

    private void SetPlayerInViewRange(bool value)
    {
        if (IsSecurity)
        {
            _playerInViewRange = true;
            return;
        }

        if (_playerInViewRange == value)
            return;

        _playerInViewRange = value;
        PlayerInViewRangeFlagChange?.Invoke();
    }

    private void SetPlayerInDiveRange(bool value)
    {
        if (_playerInDiveRange == value)
            return;

        _playerInDiveRange = value;
        PlayerInReachFlagChange?.Invoke();
    }

    private void SetPlayerInCatchRange(bool value)
    {
        if (_playerInCatchRange == value)
            return;

        _playerInCatchRange = value;
        PlayerInCatchRangeFlagChange?.Invoke();
    }

    public bool EnoughSpaceToDive()
    {
        LazyInstantiate();

        if (!TryGetSpaceRect(out Vector3 center, out Quaternion rotation, out Vector3 halfExtents))
            return false;

        Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, ~0, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hit = hits[i];

            if (ShouldIgnoreSpaceCheckCollider(hit))
                continue;

            return false;
        }

        return true;
    }

    private bool ShouldIgnoreSpaceCheckCollider(Collider hit)
    {
        if (hit == null)
            return true;

        if (hit.transform.root == transform.root)
            return true;

        if (player != null && hit.transform.root == player.transform.root)
            return true;

        return false;
    }

    private bool TryGetTargetingRect(out Vector3 center, out Quaternion rotation, out Vector3 halfExtents)
    {
        return TryGetGroundRect(targetingRectDimension, out center, out rotation, out halfExtents);
    }

    private bool TryGetGroundRect(Vector2 dimensions, out Vector3 center, out Quaternion rotation, out Vector3 halfExtents)
    {
        center = transform.position;
        rotation = transform.rotation;
        halfExtents = Vector3.zero;

        float width = Mathf.Max(0f, dimensions.x);
        float depth = Mathf.Max(0f, dimensions.y);

        if (width <= 0f || depth <= 0f)
            return false;

        if (!TryGetDiveDistanceForRect(out float diveDistance))
            return false;

        center = transform.position + transform.forward * diveDistance;
        rotation = transform.rotation;
        halfExtents = new Vector3(width * 0.5f, 0f, depth * 0.5f);

        return true;
    }

    private bool TryGetSpaceRect(out Vector3 center, out Quaternion rotation, out Vector3 halfExtents)
    {
        center = transform.position;
        rotation = transform.rotation;
        halfExtents = Vector3.zero;

        float width = Mathf.Max(0f, ensureSpaceRectDimension.x);
        float depth = Mathf.Max(0f, ensureSpaceRectDimension.y);

        if (width <= 0f || depth <= 0f)
            return false;

        if (!TryGetDiveDistanceForRect(out float diveDistance))
            return false;

        float checkHeight = capsule != null ? Mathf.Max(capsule.height, capsule.radius * 2f) : 2f;
        Vector3 checkCenterOffset = capsule != null ? capsule.center : Vector3.up * (checkHeight * 0.5f);

        center = transform.position + transform.forward * diveDistance + transform.rotation * checkCenterOffset;
        rotation = transform.rotation;
        halfExtents = new Vector3(width * 0.5f, checkHeight * 0.5f, depth * 0.5f);

        return true;
    }

    private bool TryGetDiveDistanceForRect(out float diveDistance)
    {
        if (animationMachine != null && animationMachine.TryGetDiveDistance(out diveDistance))
            return true;

        diveDistance = Mathf.Max(0f, closeRangeDiveDistanceThreshold);
        return diveDistance > 0f;
    }

    private void LazyInstantiate()
    {
        if (viewRange == null)
            viewRange = GetComponentInChildren<NpcViewRange>();

        if (player == null)
        {
            PlayerController playerController = FindFirstObjectByType<PlayerController>();
            if (playerController != null)
                player = playerController.gameObject;
        }

        if (animationMachine == null)
            animationMachine = GetComponent<NpcAnimationMachine>();

        if (capsule == null)
            capsule = GetComponent<CapsuleCollider>();

        if (controller == null)
            controller = GetComponent<NpcController>();

        if (headFollowLogic == null)
            headFollowLogic = GetComponent<HeadFollowLogic>();
    }

    private void OnDrawGizmosSelected()
    {
        LazyInstantiate();

        bool playerInsideTargetingRect = Application.isPlaying && IsPlayerInsideTargetingRect();
        bool closeRange = Application.isPlaying && IsPlayerWithinHorizontalRadius(EffectiveCloseRangeDiveDistanceThreshold);
        bool enoughSpace = !Application.isPlaying || EnoughSpaceToDive();

        if (TryGetTargetingRect(out Vector3 targetingCenter, out Quaternion targetingRotation, out Vector3 targetingHalfExtents))
        {
            Vector3 groundCenter = targetingCenter;
            groundCenter.y = transform.position.y + 0.05f;

            Gizmos.color = playerInsideTargetingRect ? Color.cyan : Color.yellow;
            DrawRect(groundCenter, targetingRotation, targetingHalfExtents.x, targetingHalfExtents.z);
        }

        if (TryGetSpaceRect(out Vector3 spaceCenter, out Quaternion spaceRotation, out Vector3 spaceHalfExtents))
        {
            Gizmos.color = enoughSpace ? Color.green : Color.red;
            DrawWireBox(spaceCenter, spaceRotation, spaceHalfExtents);

            Vector3 groundCenter = spaceCenter;
            groundCenter.y = transform.position.y + 0.1f;

            Gizmos.color = enoughSpace ? Color.green : Color.red;
            DrawRect(groundCenter, spaceRotation, spaceHalfExtents.x, spaceHalfExtents.z);
        }

        Gizmos.color = closeRange ? Color.magenta : Color.gray;
        DrawCircleXZ(transform.position + Vector3.up * 0.15f, EffectiveCloseRangeDiveDistanceThreshold);

        if (Application.isPlaying)
        {
            if (_playerInActualView && _hasActualPlayerPosition)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(_actualPlayerPosition + Vector3.up * 0.25f, 0.12f);
                Gizmos.DrawLine(transform.position + Vector3.up * 0.25f, _actualPlayerPosition + Vector3.up * 0.25f);
            }

            if (_hasPlayerPositionSnapshot)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawWireSphere(_playerPositionSnapshot + Vector3.up * 0.25f, 0.18f);
                Gizmos.DrawLine(transform.position + Vector3.up * 0.35f, _playerPositionSnapshot + Vector3.up * 0.25f);
            }

            if (_hasExitRangeSnapshot)
            {
                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(_exitRangeSnapshotPosition + Vector3.up * 0.25f, Vector3.one * 0.25f);
            }
        }
    }

    private void DrawWireBox(Vector3 center, Quaternion rotation, Vector3 halfExtents)
    {
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2f);
        Gizmos.matrix = previousMatrix;
    }

    private void DrawRect(Vector3 center, Quaternion rotation, float halfWidth, float halfDepth)
    {
        Vector3 right = rotation * Vector3.right * halfWidth;
        Vector3 forward = rotation * Vector3.forward * halfDepth;

        Vector3 frontLeft = center - right + forward;
        Vector3 frontRight = center + right + forward;
        Vector3 backRight = center + right - forward;
        Vector3 backLeft = center - right - forward;

        Gizmos.DrawLine(frontLeft, frontRight);
        Gizmos.DrawLine(frontRight, backRight);
        Gizmos.DrawLine(backRight, backLeft);
        Gizmos.DrawLine(backLeft, frontLeft);
    }

    private void DrawCircleXZ(Vector3 center, float radius, int segments = 48)
    {
        if (radius <= 0f)
            return;

        Vector3 previous = center + Vector3.forward * radius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * 2f * Mathf.PI;
            Vector3 current = center + new Vector3(Mathf.Sin(angle) * radius, 0f, Mathf.Cos(angle) * radius);
            Gizmos.DrawLine(previous, current);
            previous = current;
        }
    }
}

public enum NpcChaseTargetKind
{
    None,
    Actual,
    Snapshot
}