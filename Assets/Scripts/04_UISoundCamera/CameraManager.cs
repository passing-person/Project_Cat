using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 3f, -5f);
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private bool cameraEnabled = true;

    private void LateUpdate()
    {
        if (!cameraEnabled || target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.LookAt(target);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void SetCameraEnabled(bool value)
    {
        cameraEnabled = value;
    }

    public void SetCameraMode(GameState state)
    {
        // TODO: Adjust camera mode for Playing, Chasing, StageClear, etc.
    }

    public void Shake(float intensity, float duration)
    {
        // TODO: Add simple camera shake later.
        Debug.Log("Camera shake: " + intensity + " / " + duration);
    }
}
