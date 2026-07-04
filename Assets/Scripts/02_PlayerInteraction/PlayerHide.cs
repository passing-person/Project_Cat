using UnityEngine;

public class PlayerHide : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private HidingManager hidingManager;
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerSfxController sfxController;

    [Header("Debug")]
    public bool logHideDebug = true;

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (playerInteraction == null) playerInteraction = GetComponent<PlayerInteraction>();
        if (coreFacade == null) coreFacade = FindObjectOfType<CoreFacade>();
        if (hidingManager == null) hidingManager = FindObjectOfType<HidingManager>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        if (animationController == null) animationController = GetComponent<PlayerAnimationController>();
        if (sfxController == null) sfxController = GetComponent<PlayerSfxController>();
    }

    private void Update()
    {
        SyncForcedExitFromCore();

        if (Input.GetKeyDown(KeyCode.F))
        {
            TryHideOrExit();
        }
    }

    public void TryHideOrExit()
    {
        if (playerController != null && playerController.IsHidden)
        {
            ExitHide();
            return;
        }

        TryHide();
    }

    public void TryHide()
    {
        if (playerInteraction == null)
        {
            LogDebug(BilingualDebug.Line(
                "F 失败：PlayerInteraction 未连接",
                "F failed: PlayerInteraction is not assigned"));
            return;
        }

        IHideSpot hideSpot = playerInteraction.CurrentTarget as IHideSpot;
        if (hideSpot == null)
        {
            LogDebug(BilingualDebug.Line(
                "F 失败：当前目标不是躲藏点",
                "F failed: current target is not a hide spot"));
            return;
        }

        if (!hideSpot.CanInteract)
        {
            LogDebug(BilingualDebug.Line(
                $"F 失败：躲藏点已失效 → {hideSpot.InteractionId}",
                $"F failed: hide spot is disabled → {hideSpot.InteractionId}"));
            return;
        }

        Hide(hideSpot);
    }

    public void Hide(IHideSpot hideSpot)
    {
        if (hideSpot == null)
        {
            return;
        }

        string hideSpotId = hideSpot.InteractionId;
        bool entered;

        if (coreFacade != null)
        {
            entered = coreFacade.ReportPlayerHidden(hideSpotId);
        }
        else if (hidingManager != null)
        {
            entered = hidingManager.ReportPlayerHidden(hideSpotId);
        }
        else
        {
            LogDebug(BilingualDebug.Line(
                "F 失败：CoreFacade / HidingManager 未连接",
                "F failed: CoreFacade / HidingManager is not assigned"));
            return;
        }

        if (!entered)
        {
            LogDebug(BilingualDebug.Line(
                $"F 失败：无法进入躲藏 → {hideSpotId}",
                $"F failed: could not enter hide → {hideSpotId}"));
            return;
        }

        if (hideSpot.HidePoint != null)
        {
            transform.position = hideSpot.HidePoint.position;
        }

        if (playerController != null)
        {
            playerController.SetHidden(true);
            playerController.SetControllable(false);
        }

        playerInteraction?.ClearCurrentTarget();
        animationController?.PlayHide(true);
        sfxController?.PlayHideEnter();

        LogDebug(BilingualDebug.Line(
            $"F 成功：躲藏 → {hideSpotId}",
            $"F success: hiding → {hideSpotId}"));

        if (uiManager != null)
        {
            uiManager.ShowPrompt(BilingualDebug.Line(
                "已躲藏 - 按 F 出箱",
                "Hidden - Press F to exit"));
            uiManager.ShowHideVisual(true, GetRemainingHideTime());
        }
    }

    public void ExitHide()
    {
        CompleteLocalExit(reportToCore: true);
    }

    private void SyncForcedExitFromCore()
    {
        if (playerController == null || !playerController.IsHidden)
        {
            return;
        }

        bool coreStillHidden = coreFacade != null
            ? coreFacade.IsPlayerHidden
            : hidingManager != null && hidingManager.IsHidden;

        if (!coreStillHidden)
        {
            CompleteLocalExit(reportToCore: false);
        }
    }

    private void CompleteLocalExit(bool reportToCore)
    {
        LogDebug(BilingualDebug.Line(
            "离开躲藏",
            "Exited hide"));

        if (playerController != null)
        {
            playerController.SetHidden(false);
            playerController.SetControllable(true);
        }

        if (reportToCore)
        {
            if (coreFacade != null)
            {
                coreFacade.ReportPlayerExitHiding();
            }
            else
            {
                hidingManager?.ReportPlayerExitHiding();
            }
        }

        animationController?.PlayHide(false);
        sfxController?.PlayHideExit();

        if (uiManager != null)
        {
            uiManager.HidePrompt();
            uiManager.ShowHideVisual(false, 0f);
            uiManager.ShowInteractionNotice("Exited hiding");
        }
    }

    private float GetRemainingHideTime()
    {
        if (coreFacade != null)
        {
            return coreFacade.RemainingHideTime;
        }

        if (hidingManager != null)
        {
            return hidingManager.RemainingHideTime;
        }

        return 0f;
    }

    private void LogDebug(string message)
    {
        if (!logHideDebug)
        {
            return;
        }

        Debug.Log($"[PlayerHide] {message}", this);
    }
}
