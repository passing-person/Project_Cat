using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(Rigidbody))]
public class NpcCollisionBody : MonoBehaviour
{
    [Header("Body Collider")]
    [SerializeField] private CapsuleCollider bodyCollider;

    [Header("Rigidbody")]
    [SerializeField] private Rigidbody rb;

    private void Reset()
    {
        bodyCollider = GetComponent<CapsuleCollider>();
        rb = GetComponent<Rigidbody>();

        ConfigureDefaults();
    }

    private void Awake()
    {
        if (bodyCollider == null)
            bodyCollider = GetComponent<CapsuleCollider>();

        if (rb == null)
            rb = GetComponent<Rigidbody>();

        ConfigureDefaults();
    }

    private void ConfigureDefaults()
    {
        if (bodyCollider != null)
        {
            bodyCollider.isTrigger = false;
            bodyCollider.center = new Vector3(0f, 1f, 0f);
            bodyCollider.height = 2f;
            bodyCollider.radius = 0.35f;
        }

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
    }
}