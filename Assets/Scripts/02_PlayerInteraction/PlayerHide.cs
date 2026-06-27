using UnityEngine;

public class PlayerHide : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private HidingManager hidingManager;
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerSfxController sfxController;

    private IHideSpot currentHideSpot;

    private void Update()
    {
        SyncForcedExit();

        if (Input.GetKeyDown(KeyCode.F))
            TryHideOrExit();
    }

    private void SyncForcedExit()
    {
        if (playerController == null || !playerController.IsHidden)
            return;

        if (hidingManager == null || hidingManager.IsHidden)
            return;

        ExitHide(fromManager: true);
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
            return;

        IHideSpot hideSpot = playerInteraction.CurrentTarget as IHideSpot;
        if (hideSpot == null || !hideSpot.CanInteract)
            return;

        Hide(hideSpot);
    }

    public void Hide(IHideSpot hideSpot)
    {
        string hideSpotId = hideSpot.InteractionId;

        if (coreFacade != null)
        {
            if (!coreFacade.CanUseHideSpot(hideSpotId))
                return;

            if (!coreFacade.ReportPlayerHidden(hideSpotId))
                return;
        }
        else if (hidingManager != null)
        {
            if (!hidingManager.CanUseHideSpot(hideSpotId))
                return;

            if (!hidingManager.ReportPlayerHidden(hideSpotId))
                return;
        }

        currentHideSpot = hideSpot;

        if (hideSpot.HidePoint != null)
            transform.position = hideSpot.HidePoint.position;

        if (playerController != null)
        {
            playerController.SetHidden(true);
            playerController.SetControllable(false);
        }

        playerInteraction?.ClearCurrentTarget();
        animationController?.PlayHide(true);
        sfxController?.PlayHideEnter();

        if (uiManager != null)
            uiManager.ShowPrompt("Hidden - Press F to exit");
    }

    public void ExitHide(bool fromManager = false)
    {
        currentHideSpot = null;

        if (playerController != null)
        {
            playerController.SetHidden(false);
            playerController.SetControllable(true);
        }

        if (!fromManager)
        {
            if (coreFacade != null)
                coreFacade.ReportPlayerExitHiding();
            else
                hidingManager?.ReportPlayerExitHiding();
        }

        animationController?.PlayHide(false);
        sfxController?.PlayHideExit();

        if (uiManager != null)
            uiManager.HidePrompt();
    }
}
