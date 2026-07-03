using UnityEngine;

[DisallowMultipleComponent]
public class SeatLogic : MonoBehaviour
{
    [Header("Seat Anchor")]
    [Tooltip("Where the NPC's bottom should be placed when seated.")]
    [SerializeField] private Transform bottomPoint;

    [Tooltip("Optional explicit facing direction while seated. If null, uses bottomPoint.forward or seat forward.")]
    [SerializeField] private Transform facingPoint;


    [Header("Debug")]
    [SerializeField] private bool showGizmos = true;

    public Vector3 BottomPosition
    {
        get
        {
            if (bottomPoint != null)
                return bottomPoint.position;

            return transform.position;
        }
    }

    public Quaternion SittingRotation
    {
        get
        {
            if (facingPoint != null)
                return Quaternion.LookRotation(FlattenForward(facingPoint.forward), Vector3.up);

            if (bottomPoint != null)
                return Quaternion.LookRotation(FlattenForward(bottomPoint.forward), Vector3.up);

            return Quaternion.LookRotation(FlattenForward(transform.forward), Vector3.up);
        }
    }

    public bool HasValidBottomPoint => bottomPoint != null;

    private Vector3 FlattenForward(Vector3 forward)
    {
        forward.y = 0f;

        if (forward.sqrMagnitude <= 0.0001f)
            return Vector3.forward;

        return forward.normalized;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        Vector3 pos = BottomPosition;
        Quaternion rot = SittingRotation;

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(pos, 0.08f);

        Gizmos.color = Color.blue;
        Gizmos.DrawLine(pos, pos + rot * Vector3.forward * 1f);
    }
#endif
}