using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class HeadFollowLogic : MonoBehaviour
{
    [Header("References")]
    [SerializeField] NpcView view;
    [SerializeField] Transform headReference;
    [SerializeField] NpcAnimationMachine animMachine;

    [Header("Head Follow Params")]
    [SerializeField, Min(0.01f)] private float headDelayInterval = 0.2f;
    [SerializeField, Min(0f)] private float targetSmoothSpeed = 8f;
    [SerializeField, Min(0f)] private float playerLookHeightOffset = 1.35f;

    [Header("IK Weight")]
    [SerializeField, Range(0f, 1f)] private float activeLookWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float scanLookWeight = 0.45f;
    [SerializeField, Min(0f)] private float weightBlendSpeed = 6f;

    [Header("IK Body Distribution")]
    [SerializeField, Range(0f, 1f)] private float bodyWeight = 0.1f;
    [SerializeField, Range(0f, 1f)] private float headWeight = 0.9f;
    [SerializeField, Range(0f, 1f)] private float eyesWeight = 1f;
    [SerializeField, Range(0f, 1f)] private float clampWeight = 0.5f;

    [Header("Casual Scan")]
    [SerializeField] private bool scanWhenPlayerNotInView = true;
    [SerializeField, Min(0.1f)] private float scanDistance = 4f;
    [SerializeField] private float scanHeightOffset = 1.4f;
    [SerializeField, Range(0f, 120f)] private float scanYawAngle = 45f;
    [SerializeField, Min(0.01f)] private float scanSpeed = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugTarget;

    private Animator animator;
    private Transform lookTarget;

    private Coroutine delayedFollowRoutine;

    private Vector3 desiredLookPosition;
    private float currentLookWeight;
    private float targetLookWeight;

    private bool playerCurrentlyTracked;

    private void Awake()
    {
        animator = GetComponent<Animator>();

        if (view == null)
            view = GetComponentInParent<NpcView>();

        if (headReference == null && animator != null && animator.isHuman)
            headReference = animator.GetBoneTransform(HumanBodyBones.Head);

        if (headReference == null)
            headReference = transform;

        if (animMachine == null)
            animMachine = GetComponentInParent<NpcAnimationMachine>();

        CreateLookTarget();
        desiredLookPosition = GetScanPosition();
        lookTarget.position = desiredLookPosition;
    }

    private void OnEnable()
    {
        delayedFollowRoutine ??= StartCoroutine(HeadFollowCoroutine());
    }

    private void OnDisable()
    {
        if (delayedFollowRoutine != null)
        {
            StopCoroutine(delayedFollowRoutine);
            delayedFollowRoutine = null;
        }

        currentLookWeight = 0f;
        targetLookWeight = 0f;
    }

    private void LateUpdate()
    {
        if (lookTarget == null)
            return;

        lookTarget.position = Vector3.Lerp(
            lookTarget.position,
            desiredLookPosition,
            targetSmoothSpeed * Time.deltaTime
        );

        currentLookWeight = Mathf.MoveTowards(
            currentLookWeight,
            targetLookWeight,
            weightBlendSpeed * Time.deltaTime
        );
    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (animator == null || lookTarget == null)
            return;

        if (animMachine != null && !animMachine.AnimatorIKActive)
        {
            animator.SetLookAtWeight(0f);
            return;
        }

        if (currentLookWeight <= 0.001f)
        {
            animator.SetLookAtWeight(0f);
            return;
        }

        animator.SetLookAtWeight(
            currentLookWeight,
            bodyWeight,
            headWeight,
            eyesWeight,
            clampWeight
        );

        animator.SetLookAtPosition(lookTarget.position);
    }

    private IEnumerator HeadFollowCoroutine()
    {
        WaitForSeconds wait = new WaitForSeconds(headDelayInterval);

        while (enabled)
        {
            RefreshDesiredLookPosition();
            yield return wait;
        }
    }

    private void RefreshDesiredLookPosition()
    {
        if (view != null && view.PlayerInView)
        {
            if (view.TryGetPlayerPositionSnapshot(out Vector3 playerPosition))
            {
                desiredLookPosition = playerPosition + Vector3.up * playerLookHeightOffset;
                targetLookWeight = activeLookWeight;
                playerCurrentlyTracked = true;
                return;
            }
        }

        playerCurrentlyTracked = false;

        if (scanWhenPlayerNotInView)
        {
            desiredLookPosition = GetScanPosition();
            targetLookWeight = scanLookWeight;
        }
        else
        {
            targetLookWeight = 0f;
        }
    }

    private Vector3 GetScanPosition()
    {
        Transform reference = headReference != null ? headReference : transform;

        float yaw = Mathf.Sin(Time.time * scanSpeed * Mathf.PI * 2f) * scanYawAngle;

        Quaternion scanRotation = Quaternion.AngleAxis(yaw, Vector3.up);
        Vector3 scanDirection = scanRotation * transform.forward;

        Vector3 origin = transform.position + Vector3.up * scanHeightOffset;

        return origin + scanDirection.normalized * scanDistance;
    }

    private void CreateLookTarget()
    {
        if (lookTarget != null)
            return;

        GameObject targetObject = new GameObject($"{name}_HeadLookTarget");
        targetObject.transform.SetParent(transform.root);
        targetObject.hideFlags = showDebugTarget ? HideFlags.None : HideFlags.HideInHierarchy;

        lookTarget = targetObject.transform;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (lookTarget == null)
            return;

        Gizmos.color = playerCurrentlyTracked ? Color.red : Color.yellow;
        Gizmos.DrawSphere(lookTarget.position, 0.08f);

        Transform reference = headReference != null ? headReference : transform;
        Gizmos.DrawLine(reference.position, lookTarget.position);
    }
#endif
}