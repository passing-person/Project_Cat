using UnityEngine;

public class NpcPerception : MonoBehaviour
{
    [SerializeField] private float viewDistance = 8f;
    [SerializeField] private float viewAngle = 120f;

    public bool CanSeePlayer(PlayerController player)
    {
        if (player == null || player.IsHidden)
            return false;

        Vector3 toPlayer = player.transform.position - transform.position;

        if (toPlayer.magnitude > viewDistance)
            return false;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        return angle <= viewAngle * 0.5f;
    }
}
