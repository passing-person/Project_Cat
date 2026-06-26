using UnityEngine;
using UnityEngine.UIElements;

public class NpcView : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] NpcViewParams viewParams;

    // components
    private NpcViewRange viewRange;

    // out API
    public bool PlayerInView => TryFindPlayerInView();

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

    private bool TryFindPlayerInView()
    {
        if (!viewRange.playerInViewRange) return false;
        Vector3 toPlayer = player.transform.position - transform.position;
        bool withinHeight = Mathf.Abs(toPlayer.y) < SectorHeight / 2;
        bool withinDeg = Mathf.Abs(Vector3.Angle(transform.forward, toPlayer)) < SectorDeg / 2;
        bool withinRadius = Vector3.Magnitude(toPlayer) < SectorRadius;
        return (withinRadius && withinDeg && withinHeight);
    }

    private void LazyInstantiate()
    {
        if (viewRange == null) viewRange = gameObject.GetComponentInChildren<NpcViewRange>();
    }
}
