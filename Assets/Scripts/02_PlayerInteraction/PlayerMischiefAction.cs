using UnityEngine;

public class PlayerMischiefAction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private MischiefManager mischiefManager;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerSfxController sfxController;
    [SerializeField] private UIManager uiManager;

    [Header("Debug")]
    public bool logMischiefDebug = true;

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerInteraction == null) playerInteraction = GetComponent<PlayerInteraction>();
        if (coreFacade == null) coreFacade = FindObjectOfType<CoreFacade>();
        if (mischiefManager == null) mischiefManager = FindObjectOfType<MischiefManager>();
        if (animationController == null) animationController = GetComponent<PlayerAnimationController>();
        if (sfxController == null) sfxController = GetComponent<PlayerSfxController>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            TryPerformMischief();
        }
    }

    public void TryPerformMischief()
    {
        if (playerInteraction == null)
        {
            LogDebug(BilingualDebug.Line(
                "左键失败：PlayerInteraction 未连接",
                "LMB failed: PlayerInteraction is not assigned"));
            return;
        }

        if (coreFacade == null && mischiefManager == null)
        {
            LogDebug(BilingualDebug.Line(
                "左键失败：CoreFacade / MischiefManager 未连接",
                "LMB failed: CoreFacade / MischiefManager is not assigned"));
            return;
        }

        if (playerController != null && !playerController.IsControllable)
        {
            LogDebug(BilingualDebug.Line(
                "左键失败：玩家不可控制",
                "LMB failed: player is not controllable"));
            return;
        }

        if (playerController != null && playerController.IsHidden)
        {
            LogDebug(BilingualDebug.Line(
                "左键失败：玩家正在躲藏",
                "LMB failed: player is hidden"));
            return;
        }

        if (playerInteraction.CurrentTarget == null)
        {
            LogDebug(BilingualDebug.Line(
                "左键失败：没有交互目标",
                "LMB failed: no interaction target"));
            return;
        }

        IMischiefTarget target = playerInteraction.CurrentTarget as IMischiefTarget;
        if (target == null)
        {
            LogDebug(BilingualDebug.Line(
                $"左键失败：当前目标不是捣乱点 → {playerInteraction.CurrentTarget.InteractionId}",
                $"LMB failed: current target is not a mischief target → {playerInteraction.CurrentTarget.InteractionId}"));
            return;
        }

        if (!target.CanInteract)
        {
            LogDebug(BilingualDebug.Line(
                $"左键失败：目标不可交互 → {target.InteractionId}",
                $"LMB failed: target not interactable → {target.InteractionId}"));
            return;
        }

        if (!CanApplyMischief(target.InteractionId))
        {
            LogDebug(BilingualDebug.Line(
                $"左键失败：Core 拒绝 → {target.InteractionId}，状态={GetTargetState(target.InteractionId)}",
                $"LMB failed: Core rejected → {target.InteractionId}, state={GetTargetState(target.InteractionId)}"));
            return;
        }

        string actorId = playerController != null ? playerController.PlayerId : "Player";
        MischiefContext context = target.CreateContext(actorId);
        bool applied = ApplyMischief(context);

        if (!applied)
        {
            LogDebug(BilingualDebug.Line(
                $"左键失败：ApplyMischief 返回 false → {target.InteractionId}",
                $"LMB failed: ApplyMischief returned false → {target.InteractionId}"));
            return;
        }

        LogDebug(BilingualDebug.Line(
            $"左键成功：捣乱 → {target.InteractionId}，怒气 +{context.BaseRageAmount}",
            $"LMB success: mischief → {target.InteractionId}, rage +{context.BaseRageAmount}"));
        uiManager?.ShowMischiefApplied(target.InteractionId, context.BaseRageAmount);
        animationController?.PlayMischief();
        sfxController?.PlayMischief();
    }

    private bool CanApplyMischief(string targetId)
    {
        if (coreFacade != null)
        {
            return coreFacade.CanApplyMischief(targetId);
        }

        return mischiefManager != null && mischiefManager.CanApplyMischief(targetId);
    }

    private MischiefTargetState GetTargetState(string targetId)
    {
        if (coreFacade != null)
        {
            return coreFacade.GetMischiefTargetState(targetId);
        }

        return mischiefManager != null ? mischiefManager.GetMischiefTargetState(targetId) : MischiefTargetState.Disabled;
    }

    private bool ApplyMischief(MischiefContext context)
    {
        if (coreFacade != null)
        {
            return coreFacade.ApplyMischief(context);
        }

        return mischiefManager != null && mischiefManager.ApplyMischief(context);
    }

    private void LogDebug(string message)
    {
        if (!logMischiefDebug)
        {
            return;
        }

        Debug.Log($"[PlayerMischief] {message}", this);
    }
}
