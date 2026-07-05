using System.Collections;
using UnityEngine;

public class TrashCubeSpawner : MonoBehaviour
{
    [Header("Prefab")]
    public GameObject trashCubePrefab;

    [Header("Spawn Point")]
    public Transform emitPoint;

    [Header("Emission")]
    public float emissionDuration = 2.6f;
    public float minSpawnInterval = 0.035f;
    public float maxSpawnInterval = 0.09f;

    [Header("Random Size")]
    public float minScale = 0.015f;
    public float maxScale = 0.05f;
    public float particleSizeScale = 1f;

    [Header("Throw")]
    public float minForce = 0.8f;
    public float maxForce = 2.2f;
    [Range(0f, 180f)]
    public float fanAngle = 70f;
    [Range(0f, 80f)]
    public float launchAngle = 38f;
    public float spawnJitter = 0.08f;
    public float maxUpwardVelocity = 2.4f;

    [Header("Lifetime")]
    public float minLifeTime = 2.2f;
    public float maxLifeTime = 4f;

    private Coroutine spawnRoutine;
    private float emitEndTime;

    // Called by the cat action animation event.
    public void AE_SpawnTrashCubes()
    {
        SpawnTrashCubes();
    }

    public void AE_StopTrashCubes()
    {
        StopTrashCubes();
    }

    public void SpawnTrashCubes()
    {
        if (trashCubePrefab == null || emitPoint == null)
            return;

        emitEndTime = Time.time + Mathf.Max(0f, emissionDuration);

        if (emissionDuration <= 0f)
            return;

        if (spawnRoutine != null)
            return;

        spawnRoutine = StartCoroutine(SpawnTrashCubesOverTime());
    }

    public void StopTrashCubes()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }
    }

    private IEnumerator SpawnTrashCubesOverTime()
    {
        while (Time.time < emitEndTime)
        {
            SpawnSingleTrashCube();
            yield return new WaitForSeconds(Random.Range(minSpawnInterval, maxSpawnInterval));
        }

        spawnRoutine = null;
    }

    private void SpawnSingleTrashCube()
    {
        Vector3 jitter = Random.insideUnitSphere * spawnJitter;
        jitter.y = Mathf.Abs(jitter.y) * 0.4f;

        GameObject cube = Instantiate(
            trashCubePrefab,
            emitPoint.position + jitter,
            Random.rotation
        );

        float safeSizeScale = Mathf.Max(0f, particleSizeScale);
        float safeMinScale = Mathf.Min(minScale * safeSizeScale, 0.05f);
        float safeMaxScale = Mathf.Min(Mathf.Max(maxScale * safeSizeScale, safeMinScale), 0.05f);
        float scale = Random.Range(safeMinScale, safeMaxScale);
        cube.transform.localScale = Vector3.one * scale;

        Rigidbody rb = cube.GetComponent<Rigidbody>();
        if (rb != null)
        {
            float speed = Random.Range(minForce, maxForce);
            float yaw = Random.Range(fanAngle * -0.5f, fanAngle * 0.5f);
            float pitchRadians = launchAngle * Mathf.Deg2Rad;
            Vector3 fanDirection = Quaternion.AngleAxis(yaw, Vector3.up) * transform.forward;

            rb.velocity =
                fanDirection.normalized * (speed * Mathf.Cos(pitchRadians)) +
                Vector3.up * Mathf.Min(speed * Mathf.Sin(pitchRadians), maxUpwardVelocity);

            rb.angularVelocity = Random.insideUnitSphere * Random.Range(4f, 12f);
        }

        Destroy(cube, Random.Range(minLifeTime, maxLifeTime));
    }
}
