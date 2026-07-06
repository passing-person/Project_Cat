using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SeatLogic : MonoBehaviour
{
    private enum SeatMode
    {
        SingleSeat,
        MultipleSeats
    }

    [System.Serializable]
    private struct SeatAnchor
    {
        [Tooltip("Where the NPC's bottom should be placed when seated.")]
        public Transform bottomPoint;

        [Tooltip("Optional explicit facing direction while seated. If null, uses bottomPoint.forward or sofa forward.")]
        public Transform facingPoint;

        public bool HasValidBottomPoint => bottomPoint != null;

        public Vector3 ResolveBottomPosition(Transform fallback)
        {
            return bottomPoint != null ? bottomPoint.position : fallback.position;
        }

        public Quaternion ResolveSittingRotation(Transform fallback)
        {
            if (facingPoint != null)
                return Quaternion.LookRotation(FlattenForward(facingPoint.forward), Vector3.up);

            if (bottomPoint != null)
                return Quaternion.LookRotation(FlattenForward(bottomPoint.forward), Vector3.up);

            return Quaternion.LookRotation(FlattenForward(fallback.forward), Vector3.up);
        }
    }

    [Header("Seat Mode")]
    [SerializeField] private SeatMode seatMode = SeatMode.SingleSeat;

    [Tooltip("If true, a multiple-seat sofa selects one valid seat on enable. If false, selection is lazy on first BottomPosition/SittingRotation read.")]
    [SerializeField] private bool randomizeOnEnable = true;

    [Header("Single Seat Anchor")]
    [Tooltip("Where the NPC's bottom should be placed when seated.")]
    [SerializeField] private Transform bottomPoint;

    [Tooltip("Optional explicit facing direction while seated. If null, uses bottomPoint.forward or sofa forward.")]
    [SerializeField] private Transform facingPoint;

    [Header("Multiple Seat Anchors")]
    [Tooltip("Used only when Seat Mode is MultipleSeats. Each entry is one possible sitting point.")]
    [SerializeField] private List<SeatAnchor> seatAnchors = new();

    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;
    [SerializeField] private bool showOnlySelectedSeat = false;

    private int selectedSeatIndex = -1;

    /// <summary>
    /// NPC-facing API. Unchanged.
    /// </summary>
    public Vector3 BottomPosition
    {
        get
        {
            SeatAnchor seat = ResolveCurrentSeat();
            return seat.ResolveBottomPosition(transform);
        }
    }

    /// <summary>
    /// NPC-facing API. Unchanged.
    /// </summary>
    public Quaternion SittingRotation
    {
        get
        {
            SeatAnchor seat = ResolveCurrentSeat();
            return seat.ResolveSittingRotation(transform);
        }
    }

    /// <summary>
    /// NPC-facing API. Unchanged.
    /// </summary>
    public bool HasValidBottomPoint
    {
        get
        {
            if (seatMode == SeatMode.SingleSeat)
                return bottomPoint != null;

            return HasAnyValidSeatAnchor();
        }
    }

    public bool IsMultipleSeatsMode => seatMode == SeatMode.MultipleSeats;
    public int SelectedSeatIndex => selectedSeatIndex;

    private void OnEnable()
    {
        if (seatMode == SeatMode.MultipleSeats && randomizeOnEnable)
            SelectRandomSeat();
    }

    /// <summary>
    /// Optional external API. NPCs do not need to call this.
    /// Useful for debug tools or for forcing a new random point between seating cycles.
    /// </summary>
    public void SelectRandomSeat()
    {
        selectedSeatIndex = PickRandomValidSeatIndex();
    }

    /// <summary>
    /// Optional external API. NPCs do not need to call this.
    /// Next BottomPosition/SittingRotation read will lazily pick a valid random seat.
    /// </summary>
    public void ClearSelectedSeat()
    {
        selectedSeatIndex = -1;
    }

    private SeatAnchor ResolveCurrentSeat()
    {
        if (seatMode == SeatMode.SingleSeat)
        {
            return new SeatAnchor
            {
                bottomPoint = bottomPoint,
                facingPoint = facingPoint
            };
        }

        EnsureValidSelectedSeat();

        if (selectedSeatIndex >= 0 && selectedSeatIndex < seatAnchors.Count)
            return seatAnchors[selectedSeatIndex];

        // Safe fallback: preserve old behavior if no multiple-seat entry is valid.
        return new SeatAnchor
        {
            bottomPoint = bottomPoint,
            facingPoint = facingPoint
        };
    }

    private void EnsureValidSelectedSeat()
    {
        if (seatMode != SeatMode.MultipleSeats)
            return;

        if (IsValidSeatIndex(selectedSeatIndex))
            return;

        selectedSeatIndex = PickRandomValidSeatIndex();
    }

    private int PickRandomValidSeatIndex()
    {
        List<int> validIndices = new();

        for (int i = 0; i < seatAnchors.Count; i++)
        {
            if (seatAnchors[i].HasValidBottomPoint)
                validIndices.Add(i);
        }

        if (validIndices.Count <= 0)
            return -1;

        int randomListIndex = Random.Range(0, validIndices.Count);
        return validIndices[randomListIndex];
    }

    private bool IsValidSeatIndex(int index)
    {
        return index >= 0 &&
               index < seatAnchors.Count &&
               seatAnchors[index].HasValidBottomPoint;
    }

    private bool HasAnyValidSeatAnchor()
    {
        for (int i = 0; i < seatAnchors.Count; i++)
        {
            if (seatAnchors[i].HasValidBottomPoint)
                return true;
        }

        return false;
    }

    private static Vector3 FlattenForward(Vector3 forward)
    {
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            return Vector3.forward;

        return forward.normalized;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (seatMode == SeatMode.SingleSeat)
        {
            selectedSeatIndex = -1;
            return;
        }

        if (!IsValidSeatIndex(selectedSeatIndex))
            selectedSeatIndex = -1;
    }

    [ContextMenu("Select Random Seat")]
    private void DebugSelectRandomSeat()
    {
        SelectRandomSeat();
    }

    [ContextMenu("Clear Selected Seat")]
    private void DebugClearSelectedSeat()
    {
        ClearSelectedSeat();
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        if (seatMode == SeatMode.SingleSeat)
        {
            DrawSeatGizmo(
                bottomPoint != null ? bottomPoint.position : transform.position,
                ResolveSingleSeatRotation(),
                Color.cyan,
                Color.blue
            );

            return;
        }

        EnsureValidSelectedSeat();

        for (int i = 0; i < seatAnchors.Count; i++)
        {
            if (showOnlySelectedSeat && i != selectedSeatIndex)
                continue;

            if (!seatAnchors[i].HasValidBottomPoint)
                continue;

            bool selected = i == selectedSeatIndex;

            Vector3 pos = seatAnchors[i].ResolveBottomPosition(transform);
            Quaternion rot = seatAnchors[i].ResolveSittingRotation(transform);

            DrawSeatGizmo(
                pos,
                rot,
                selected ? Color.green : Color.cyan,
                selected ? Color.yellow : Color.blue
            );
        }
    }

    private Quaternion ResolveSingleSeatRotation()
    {
        SeatAnchor seat = new SeatAnchor
        {
            bottomPoint = bottomPoint,
            facingPoint = facingPoint
        };

        return seat.ResolveSittingRotation(transform);
    }

    private static void DrawSeatGizmo(Vector3 pos, Quaternion rot, Color sphereColor, Color forwardColor)
    {
        Gizmos.color = sphereColor;
        Gizmos.DrawSphere(pos, 0.08f);

        Gizmos.color = forwardColor;
        Gizmos.DrawLine(pos, pos + rot * Vector3.forward * 1f);
    }
#endif
}
