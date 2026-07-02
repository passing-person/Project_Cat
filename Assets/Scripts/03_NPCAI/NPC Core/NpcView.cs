using UnityEngine;
using System;

public class NpcView : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] private NpcViewParams viewParams;

    [Header("Body parts")]
    [SerializeField] private GameObject head;

    [Header("Reach")]
    [SerializeField, Min(0f)] private float diveTriggerRadius = 1.75f;
    [SerializeField, Min(0f)] private float catchRadius = 0.75f;

    [Tooltip("If true, dive range only counts while the player is inside the view range trigger.")]
    [SerializeField] private bool requireViewRangeForDive = true;

    [Tooltip("If true, catch range only counts while the player is inside the view range trigger.")]
    [SerializeField] private bool requireViewRangeForCatch = false;

    private Transform headTransform => head != null ? head.transform : transform;

    private NpcViewRange viewRange;
    private GameObject player;

    public event Action PlayerInViewFlagChange;
    public event Action PlayerInReachFlagChange;
    public event Action PlayerInCatchRangeFlagChange;

    public bool PlayerInView => _playerInView;
    public bool PlayerInReach => _playerInDiveRange;
    public bool PlayerInCatchRange => _playerInCatchRange;

    public Vector3 PlayerPosition => player != null ? player.transform.position : transform.position;

    private bool _playerInView;
    private bool _playerInDiveRange;
    private bool _playerInCatchRange;

    private float SectorRadius => viewParams.sectorRadius;
    private float SectorDeg => viewParams.sectorDeg;

    private void Awake()
    {
        LazyInstantiate();
    }

    private void Start()
    {
        LazyInstantiate();
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        LazyInstantiate();

        if (player == null || viewRange == null)
        {
            SetPlayerInView(false);
            SetPlayerInDiveRange(false);
            SetPlayerInCatchRange(false);
            return;
        }

        UpdatePlayerVisibility();
        UpdatePlayerDiveRange();
        UpdatePlayerCatchRange();
    }

    private void UpdatePlayerVisibility()
    {
        if (_playerInView)
        {
            if (!viewRange.playerInViewRange)
                SetPlayerInView(false);

            return;
        }

        if (!viewRange.playerInViewRange)
            return;

        Vector3 toPlayer = player.transform.position - transform.position;

        bool withinDeg = Vector3.Angle(headTransform.forward, toPlayer) < SectorDeg * 0.5f;
        bool withinRadius = toPlayer.sqrMagnitude <= SectorRadius * SectorRadius;

        if (withinDeg && withinRadius)
            SetPlayerInView(true);
    }

    private void UpdatePlayerDiveRange()
    {
        if (requireViewRangeForDive && !viewRange.playerInViewRange)
        {
            SetPlayerInDiveRange(false);
            return;
        }

        SetPlayerInDiveRange(IsPlayerWithinHorizontalRadius(diveTriggerRadius));
    }

    private void UpdatePlayerCatchRange()
    {
        if (requireViewRangeForCatch && !viewRange.playerInViewRange)
        {
            SetPlayerInCatchRange(false);
            return;
        }

        SetPlayerInCatchRange(IsPlayerWithinHorizontalRadius(catchRadius));
    }

    private bool IsPlayerWithinHorizontalRadius(float radius)
    {
        if (player == null)
            return false;

        Vector3 toPlayer = player.transform.position - transform.position;
        toPlayer.y = 0f;

        return toPlayer.sqrMagnitude <= radius * radius;
    }

    private void SetPlayerInView(bool value)
    {
        if (_playerInView == value)
            return;

        _playerInView = value;
        PlayerInViewFlagChange?.Invoke();
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
    }
}