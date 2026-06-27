using UnityEngine;

public class PlayerMischiefAction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private MischiefManager mischiefManager;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerSfxController sfxController;

    [Header("Debug")]
    public bool logMischiefDebug = true;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryPerformMischief();
    }

    public void TryPerformMischief()
    {
        if (playerInteraction == null)
        {
            LogDebug("左键失败：PlayerInteraction 未连接");
            return;
        }

        if (mischiefManager == null)
        {
            LogDebug("左键失败：MischiefManager 未连接");
            return;
        }

        if (playerController != null && !playerController.IsControllable)
        {
            LogDebug("左键失败：玩家不可控制");
            return;
        }

        if (playerController != null && playerController.IsHidden)
        {
            LogDebug("左键失败：玩家正在躲藏");
            return;
        }

        if (playerInteraction.CurrentTarget == null)
        {
            LogDebug("左键失败：没有交互目标（请靠近键盘/电话）");
            return;
        }

        IMischiefTarget target = playerInteraction.CurrentTarget as IMischiefTarget;
        if (target == null)
        {
            LogDebug($"左键失败：当前目标不是捣乱点 → {playerInteraction.CurrentTarget.InteractionId}");
            return;
        }

        if (!target.CanInteract)
        {
            LogDebug($"左键失败：目标不可交互 → {target.InteractionId}");
            return;
        }

        if (!mischiefManager.CanApplyMischief(target.InteractionId))
        {
            LogDebug($"左键失败：MischiefManager 拒绝 → {target.InteractionId}，状态={mischiefManager.GetMischiefTargetState(target.InteractionId)}");
            return;
        }

        string actorId = playerController != null ? playerController.PlayerId : "Player";
        MischiefContext context = target.CreateContext(actorId);
        bool applied = mischiefManager.ApplyMischief(context);

        if (!applied)
        {
            LogDebug($"左键失败：ApplyMischief 返回 false → {target.InteractionId}");
            return;
        }

        LogDebug($"左键成功：捣乱 → {target.InteractionId}，怒气 +{context.BaseRageAmount}");
        animationController?.PlayMischief();
        sfxController?.PlayMischief();
    }

    private void LogDebug(string message)
    {
        if (!logMischiefDebug)
            return;

        Debug.Log($"[PlayerMischief] {message}", this);
    }
}
