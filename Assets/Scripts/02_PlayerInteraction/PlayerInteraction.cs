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
            {
                LogDebug(BilingualDebug.Line(
                    "扫描：范围内没有可交互目标，清除当前目标",
                    "Scan: no interactable in range, clearing current target"));
            }

            if (CurrentTarget != null)
                ClearCurrentTarget();

            return;
        }

        if (CurrentTarget != best)
        {
            mischiefPromptUnlocked = false;
            LogDebug(BilingualDebug.Line(
                $"扫描选中目标: {((MonoBehaviour)best).name}，距离 {bestDistance:0.00}m",
                $"Scan selected target: {((MonoBehaviour)best).name}, distance {bestDistance:0.00}m"));
            SetCurrentTarget(best);
        }
    }

    private void ValidateCurrentTarget()
    {
        if (CurrentTarget == null)
            return;

        if (playerController != null && playerController.IsHidden)
        {
            LogDebug(BilingualDebug.Line(
                "验证失败：玩家正在躲藏",
                "Validation failed: player is hidden"));
            ClearCurrentTarget();
            return;
        }

        if (!CurrentTarget.CanInteract)
        {
            LogDebug(BilingualDebug.Line(
                $"验证失败：目标不可交互 {GetTargetName(CurrentTarget)}",
                $"Validation failed: target not interactable {GetTargetName(CurrentTarget)}"));
            ClearCurrentTarget();
            return;
        }

        if (!IsWithinRange(CurrentTarget, out float distance))
        {
            LogDebug(BilingualDebug.Line(
                $"验证失败：目标过远 {GetTargetName(CurrentTarget)}，距离 {distance:0.00}m",
                $"Validation failed: target too far {GetTargetName(CurrentTarget)}, distance {distance:0.00}m"));
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
            LogDebug(BilingualDebug.Line(
                "E 失败：玩家正在躲藏",
                "E failed: player is hidden"));
            return false;
        }

        if (CurrentTarget == null)
        {
            LogDebug(BilingualDebug.Line(
                "E 失败：没有当前交互目标（请靠近键盘/电话/纸箱）",
                "E failed: no current target (move near keyboard, phone, or hide box)"));
            return false;
        }

        if (!CurrentTarget.CanInteract)
        {
            LogDebug(BilingualDebug.Line(
                $"E 失败：目标不可交互 {GetTargetName(CurrentTarget)}",
                $"E failed: target not interactable {GetTargetName(CurrentTarget)}"));
            return false;
        }

        if (!IsWithinRange(CurrentTarget, out float distance))
        {
            LogDebug(BilingualDebug.Line(
                $"E 失败：距离过远 {GetTargetName(CurrentTarget)}，{distance:0.00}m / 需要 ≤{interactionRange:0.00}m",
                $"E failed: too far {GetTargetName(CurrentTarget)}, {distance:0.00}m / need ≤{interactionRange:0.00}m"));
            return false;
        }

        if (CurrentTarget is IMischiefTarget)
        {
            mischiefPromptUnlocked = true;
            LogDebug(BilingualDebug.Line(
                $"E 成功：已解锁捣乱提示 → {GetTargetName(CurrentTarget)}",
                $"E success: mischief hint unlocked → {GetTargetName(CurrentTarget)}"));
            RefreshPrompt();
            return true;
        }

        string actorId = playerController != null ? playerController.PlayerId : "Player";
        CurrentTarget.Interact(actorId);
        LogDebug(BilingualDebug.Line(
            $"E 成功：交互 {GetTargetName(CurrentTarget)}",
            $"E success: interacted with {GetTargetName(CurrentTarget)}"));
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
            uiManager.ShowPrompt(BilingualDebug.Line("[F] 躲藏", "[F] Hide"));
            return;
        }

        if (CurrentTarget is IMischiefTarget)
        {
            if (mischiefPromptUnlocked)
                uiManager.ShowPrompt(BilingualDebug.Line("[左键] 捣乱", "[LMB] Mischief"));
            else
                uiManager.ShowPrompt(BilingualDebug.Line("[E] 交互", "[E] Interact"));
            return;
        }

        uiManager.ShowPrompt(BilingualDebug.Line("[E] 交互", "[E] Interact"));
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
