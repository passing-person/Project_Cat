using UnityEngine;
using UnityEngine.UIElements;
using System;

public class NpcView : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] NpcViewParams viewParams;

    // components
    private NpcViewRange viewRange;

    // actions
    public event Action PlayerInViewFlagChange;
    public event Action PlayerInReachFlagChange;

    // out API
    public bool PlayerInView
    {
        get
        {
            UpdatePlayerVisibility();
            return _playerInView;
        }
        private set
        {
            if (_playerInView == value)
                return;

            _playerInView = value;
            OnPlayerInViewChange();
        }
    }
        private bool _playerInView;
    public bool PlayerInReach
    {
        get
        {
            UpdatePlayerReach();
            return _playerInReach;
        }
        private set
        {
            if (_playerInReach == value)
                return;

            _playerInReach = value;
            OnPlayerInReachChange();
        }
    }
        private bool _playerInReach;
    public Vector3 PlayerPosition => player.transform.position;

    // player reference
    private GameObject player;

    // unpacked view params
    private float SectorRadius => viewParams.sectorRadius;
    private float SectorHeight => viewParams.sectorHeight;
    [Tooltip("Measured in deg.")]
    private float SectorDeg => viewParams.sectorDeg;

    private void Awake()
    {
        LazyInstantiate();
    }

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>().gameObject;
    }

    /// <summary>
    /// True once the player has been spotted.
    /// The player is only forgotten after leaving the ViewRange.
    /// </summary>
    private void UpdatePlayerVisibility()
    {
        // Already tracking.
        if (_playerInView)
        {
            if (!viewRange.playerInViewRange)
                PlayerInView = false;

            return;
        }

        // Not tracking.
        if (!viewRange.playerInViewRange)
            return;

        Vector3 toPlayer = player.transform.position - transform.position;

        bool withinHeight = Mathf.Abs(toPlayer.y) < SectorHeight * 0.5f;
        bool withinDeg = Vector3.Angle(transform.forward, toPlayer) < SectorDeg * 0.5f;
        bool withinRadius = toPlayer.sqrMagnitude < SectorRadius * SectorRadius;

        if (withinHeight && withinDeg && withinRadius)
            PlayerInView = true;
    }

    /// <summary>
    /// True while the player is within 1 unit of the NPC.
    /// Detection is only active while the player is inside the ViewRange trigger.
    /// </summary>
    private void UpdatePlayerReach()
    {
        // ViewRange gates all detection.
        if (!viewRange.playerInViewRange)
        {
            PlayerInReach = false;
            return;
        }

        Vector3 toPlayer = player.transform.position - transform.position;

        // Ignore height.
        toPlayer.y = 0f;

        PlayerInReach = toPlayer.sqrMagnitude <= 1f;
    }

    private void OnPlayerInViewChange()
    {
        PlayerInViewFlagChange?.Invoke();
    }

    private void OnPlayerInReachChange()
    {
        PlayerInReachFlagChange?.Invoke();
    }

    private void LazyInstantiate()
    {
        if (viewRange == null) viewRange = gameObject.GetComponentInChildren<NpcViewRange>();
    }
}
