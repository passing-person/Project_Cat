using UnityEngine;
using UnityEngine.UIElements;

public class NpcView : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] NpcViewParams viewParams;

    // components
    private NpcViewRange viewRange;

    // out API
    public bool PlayerInView => UpdatePlayerVisibility();
        private bool _playerInView;

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
    private bool UpdatePlayerVisibility()
    {
        // Already tracking the player.
        // Only lose them after they leave the ViewRange trigger.
        if (_playerInView)
        {
            if (!viewRange.playerInViewRange)
                _playerInView = false;

            return _playerInView;
        }

        // Not currently tracking.
        // Can only acquire if inside the view sector.
        if (!viewRange.playerInViewRange)
            return false;

        Vector3 toPlayer = player.transform.position - transform.position;

        bool withinHeight = Mathf.Abs(toPlayer.y) < SectorHeight * 0.5f;
        bool withinDeg = Vector3.Angle(transform.forward, toPlayer) < SectorDeg * 0.5f;
        bool withinRadius = toPlayer.sqrMagnitude < SectorRadius * SectorRadius;

        if (withinHeight && withinDeg && withinRadius)
            _playerInView = true;

        return _playerInView;
    }

    private void LazyInstantiate()
    {
        if (viewRange == null) viewRange = gameObject.GetComponentInChildren<NpcViewRange>();
    }
}
