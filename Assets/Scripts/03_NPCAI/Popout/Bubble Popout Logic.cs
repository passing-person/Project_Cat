using System.Collections;
using UnityEngine;

public class BubblePopoutLogic : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform npcRoot;
    [SerializeField] private Transform standingPopoutAnchor;
    [SerializeField] private Transform sittingPopoutAnchor;
    [SerializeField] private SpriteRenderer bubbleRenderer;
    [SerializeField] private SpriteRenderer contentRenderer;
    [SerializeField] private NpcBubblePopoutMap spriteMap;

    [Header("Placement")]
    [SerializeField] private float sideOffset = 0.55f;
    [SerializeField] private float upOffset = 0.7f;
    [SerializeField] private float forwardOffset = 0.05f;
    [SerializeField] private float checkRadius = 0.22f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Default")]
    [SerializeField] private bool defaultRightSide = false;

    private Camera mainCamera;
    private Coroutine activeRoutine;
    private NpcPopoutAnchorMode currentAnchorMode;

    private void Awake()
    {
        mainCamera = Camera.main;
        HideImmediate();
    }

    private void LateUpdate()
    {
        if (!gameObject.activeSelf || mainCamera == null)
            return;

        UpdatePlacement();
        FaceCamera();
    }

    public void Show(PopoutType type, float duration, NpcPopoutAnchorMode anchorMode)
    {
        if (anchorMode == NpcPopoutAnchorMode.None)
            return;

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }

        currentAnchorMode = anchorMode;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        activeRoutine = StartCoroutine(ShowRoutine(type, duration));
    }

    private Transform ResolveAnchor(NpcPopoutAnchorMode mode)
    {
        switch (mode)
        {
            case NpcPopoutAnchorMode.Standing:
                return standingPopoutAnchor;

            case NpcPopoutAnchorMode.Sitting:
                return sittingPopoutAnchor;

            case NpcPopoutAnchorMode.None:
            default:
                return null;
        }
    }

    private IEnumerator ShowRoutine(PopoutType type, float duration)
    {
        var sprite = spriteMap.GetSprite(type);

        if (sprite == null)
        {
            Debug.LogWarning($"No popout sprite mapped for type: {type}", this);
            yield break;
        }

        contentRenderer.sprite = sprite;
        gameObject.SetActive(true);

        UpdatePlacement();
        FaceCamera();

        yield return new WaitForSeconds(duration);

        HideImmediate();
        activeRoutine = null;
    }

    private void HideImmediate()
    {
        gameObject.SetActive(false);
    }

    private void UpdatePlacement()
    {
        Transform anchor = ResolveAnchor(currentAnchorMode);

        if (anchor == null || mainCamera == null)
            return;

        Vector3 axisUp = anchor.up;

        Vector3 basePos = anchor.position + axisUp * upOffset;

        Vector3 toCamera = mainCamera.transform.position - basePos;
        Vector3 planarToCamera = Vector3.ProjectOnPlane(toCamera, axisUp);

        if (planarToCamera.sqrMagnitude < 0.001f)
            planarToCamera = Vector3.ProjectOnPlane(npcRoot.forward, axisUp);

        planarToCamera.Normalize();

        Vector3 sideAxis = Vector3.Cross(axisUp, planarToCamera).normalized;

        Vector3 defaultSide = defaultRightSide ? sideAxis : -sideAxis;
        Vector3 mirrorSide = -defaultSide;

        Vector3 defaultPos =
            basePos +
            defaultSide * sideOffset +
            planarToCamera * forwardOffset;

        Vector3 mirrorPos =
            basePos +
            mirrorSide * sideOffset +
            planarToCamera * forwardOffset;

        bool defaultValid = IsValidSpace(basePos, defaultPos);
        bool mirrorValid = IsValidSpace(basePos, mirrorPos);

        if (defaultValid)
        {
            transform.position = defaultPos;
        }
        else if (mirrorValid)
        {
            transform.position = mirrorPos;
        }
        else
        {
            transform.position = basePos + planarToCamera * forwardOffset;
        }
    }

    private bool IsValidSpace(Vector3 basePos, Vector3 targetPos)
    {
        Vector3 dir = targetPos - basePos;
        float dist = dir.magnitude;

        if (dist <= 0.001f)
            return false;

        dir /= dist;

        if (Physics.SphereCast(basePos, checkRadius, dir, out _, dist, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        if (Physics.CheckSphere(targetPos, checkRadius, obstacleMask, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    private void FaceCamera()
    {
        Transform anchor = ResolveAnchor(currentAnchorMode);

        if (anchor == null || mainCamera == null)
            return;

        Vector3 direction = transform.position - mainCamera.transform.position;

        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized, anchor.up);
    }
}