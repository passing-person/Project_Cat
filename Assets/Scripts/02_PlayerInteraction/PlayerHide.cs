using UnityEngine;

public class PlayerHide : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private UIManager uiManager;

    private IHideSpot currentHideSpot;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
            TryHideOrExit();
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
        currentHideSpot = hideSpot;

        if (hideSpot.HidePoint != null)
            transform.position = hideSpot.HidePoint.position;

        if (playerController != null)
        {
            playerController.SetHidden(true);
            playerController.SetControllable(false);
        }

        if (uiManager != null)
            uiManager.ShowPrompt("Hidden - Press F to exit");
    }

    public void ExitHide()
    {
        currentHideSpot = null;

        if (playerController != null)
        {
            playerController.SetHidden(false);
            playerController.SetControllable(true);
        }

        if (uiManager != null)
            uiManager.HidePrompt();
    }
}
