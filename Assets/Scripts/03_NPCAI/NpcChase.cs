using UnityEngine;

public class NpcChase : MonoBehaviour
{
    [SerializeField] private NpcController npcController;
    [SerializeField] private Transform target;
    [SerializeField] private float chaseSpeed = 4f;

    private bool isChasing;

    private void Update()
    {
        if (!isChasing || target == null)
            return;

        UpdateChase();
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void StartChase()
    {
        isChasing = true;
    }

    public void StopChase()
    {
        isChasing = false;
    }

    private void UpdateChase()
    {
        Vector3 direction = target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Vector3 movement = direction.normalized * chaseSpeed * Time.deltaTime;
        transform.position += movement;
        transform.forward = direction.normalized;
    }
}
