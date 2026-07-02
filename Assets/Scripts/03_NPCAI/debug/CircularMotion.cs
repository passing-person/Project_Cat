using UnityEngine;

public class CircularMotion : MonoBehaviour
{
    [SerializeField] Transform center;
    [SerializeField] float radius = 3f;
    [SerializeField] float height = 1.5f;
    [SerializeField] float speed = 45f;

    float angle;

    void Update()
    {
        angle += speed * Time.deltaTime;
        float rad = angle * Mathf.Deg2Rad;

        Vector3 centerPos = center != null ? center.position : Vector3.zero;
        transform.position = centerPos + new Vector3(
            Mathf.Sin(rad) * radius,
            height,
            Mathf.Cos(rad) * radius
        );
    }
}