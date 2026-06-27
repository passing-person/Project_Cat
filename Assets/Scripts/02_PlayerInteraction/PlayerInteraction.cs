using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private Transform interactionOrigin;

    [Header("Interaction")]
    [Tooltip("扫描可交互物体的半径（比 interactionRange 大，用于发现目标）")]
    public float detectionRadius = 1.2f;

    [Tooltip("实际允许按 E / 左键的距离（到碰撞体最近点）")]
    public float interactionRange = 0.5f;

    [Header("Debug")]
    public bool logInteractionDebug = true;

    private static readonly Collider[] OverlapBuffer = new Collider[16];

    private InteractableHighlighter currentHighlighter;
    private bool mischiefPromptUnlocked;

    public IInteractable CurrentTarget { get; private set; }
    public bool IsMischiefPromptUnlocked => mischiefPromptUnlocked;

    private void Awake()
    {
        if (interactionOrigin == null)
            interactionOrigin = transform;
    }

    private void Update()
    {
        ScanForInteractable();
        ValidateCurrentTarget();

        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    private void ScanForInteractable()
    {
        if (playerController != null && playerController.IsHidden)
            return;

        int hitCount = Physics.OverlapSphereNonAlloc(
            interactionOrigin.position,
            detectionRadius,
            OverlapBuffer,
            ~0,
            QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = OverlapBuffer[i];
            if (hit == null || hit.transform.IsChildOf(transform))
                continue;

            IInteractable interactable = InteractableUtility.GetInteractable(hit);
            if (interactable == null)
                continue;

            if (!interactable.CanInteract)
                continue;

            float distance = InteractableUtility.GetDistanceToInteractable(interactionOrigin.position, interactable);
            if (distance > interactionRange)
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = interactable;
            }
        }

        if (best == null)
        {
            if (CurrentTarget != null)
                LogDebug("扫描：范围内没有可交互目标，清除当前目标");

            if (CurrentTarget != null)
                ClearCurrentTarget();

            return;
        }

        if (CurrentTarget != best)
        {
            mischiefPromptUnlocked = false;
            LogDebug($"扫描选中目标: {((MonoBehaviour)best).name}，距离 {bestDistance:0.00}m");
            SetCurrentTarget(best);
        }
    }

    private void ValidateCurrentTarget()
    {
        if (CurrentTarget == null)
            return;

        if (playerController != null && playerController.IsHidden)
        {
            LogDebug("验证失败：玩家正在躲藏");
            ClearCurrentTarget();
            return;
        }

        if (!CurrentTarget.CanInteract)
        {
            LogDebug($"验证失败：目标不可交互 {GetTargetName(CurrentTarget)}");
            ClearCurrentTarget();
            return;
        }

        if (!IsWithinRange(CurrentTarget, out float distance))
        {
            LogDebug($"验证失败：目标过远 {GetTargetName(CurrentTarget)}，距离 {distance:0.00}m");
            ClearCurrentTarget();
        }
    }

    private bool IsWithinRange(IInteractable target, out float distance)
    {
        distance = InteractableUtility.GetDistanceToInteractable(interactionOrigin.position, target);
        return distance <= interactionRange;
    }

    public void SetCurrentTarget(IInteractable target)
    {
        if (playerController != null && playerController.IsHidden)
            return;

        CurrentTarget = target;
        SetHighlighter(target);
        RefreshPrompt();
    }

    public void ClearCurrentTarget()
    {
        CurrentTarget = null;
        mischiefPromptUnlocked = false;
        ClearHighlighter();
        RefreshPrompt();
    }

    public bool TryInteract()
    {
        if (playerController != null && playerController.IsHidden)
        {
            LogDebug("E 失败：玩家正在躲藏");
            return false;
        }

        if (CurrentTarget == null)
        {
            LogDebug("E 失败：没有当前交互目标（请靠近键盘/电话/纸箱）");
            return false;
        }

        if (!CurrentTarget.CanInteract)
        {
            LogDebug($"E 失败：目标不可交互 {GetTargetName(CurrentTarget)}");
            return false;
        }

        if (!IsWithinRange(CurrentTarget, out float distance))
        {
            LogDebug($"E 失败：距离过远 {GetTargetName(CurrentTarget)}，{distance:0.00}m / 需要 ≤{interactionRange:0.00}m");
            return false;
        }

        if (CurrentTarget is IMischiefTarget)
        {
            mischiefPromptUnlocked = true;
            LogDebug($"E 成功：已解锁捣乱提示 → {GetTargetName(CurrentTarget)}");
            RefreshPrompt();
            return true;
        }

        string actorId = playerController != null ? playerController.PlayerId : "Player";
        CurrentTarget.Interact(actorId);
        LogDebug($"E 成功：交互 {GetTargetName(CurrentTarget)}");
        return true;
    }

    private void RefreshPrompt()
    {
        if (uiManager == null)
            return;

        if (CurrentTarget == null)
        {
            uiManager.HidePrompt();
            return;
        }

        if (CurrentTarget is IHideSpot && (playerController == null || !playerController.IsHidden))
        {
            uiManager.ShowPrompt("[F] 躲藏");
            return;
        }

        if (CurrentTarget is IMischiefTarget)
        {
            if (mischiefPromptUnlocked)
                uiManager.ShowPrompt("[左键] 捣乱");
            else
                uiManager.ShowPrompt("[E] 交互");
            return;
        }

        uiManager.ShowPrompt("[E] 交互");
    }

    private void SetHighlighter(IInteractable target)
    {
        ClearHighlighter();

        if (target is not MonoBehaviour behaviour)
            return;

        currentHighlighter = behaviour.GetComponent<InteractableHighlighter>();
        currentHighlighter?.ShowHighlight();
    }

    private void ClearHighlighter()
    {
        currentHighlighter?.HideHighlight();
        currentHighlighter = null;
    }

    private void LogDebug(string message)
    {
        if (!logInteractionDebug)
            return;

        Debug.Log($"[PlayerInteraction] {message}", this);
    }

    private static string GetTargetName(IInteractable target)
    {
        return target is MonoBehaviour behaviour ? behaviour.name : target.InteractionId;
    }

    private void OnDrawGizmosSelected()
    {
        if (interactionOrigin == null)
            interactionOrigin = transform;

        Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.35f);
        Gizmos.DrawWireSphere(interactionOrigin.position, detectionRadius);

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.5f);
        Gizmos.DrawWireSphere(interactionOrigin.position, interactionRange);
    }
}
