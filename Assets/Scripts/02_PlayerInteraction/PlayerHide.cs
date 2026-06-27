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

                "F 失败：当前目标不是躲藏点（请靠近纸箱 HideSpot_Box）",

                "F failed: current target is not a hide spot (move near HideSpot_Box)"));

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

        string hideSpotId = hideSpot.InteractionId;



        if (coreFacade != null)

        {

            if (!coreFacade.ReportPlayerHidden(hideSpotId))

            {

                LogDebug(BilingualDebug.Line(

                    $"F 失败：无法进入躲藏 → {hideSpotId}",

                    $"F failed: could not enter hide → {hideSpotId}"));

                return;

            }

        }

        else if (hidingManager != null)

        {

            if (!hidingManager.ReportPlayerHidden(hideSpotId))

            {

                LogDebug(BilingualDebug.Line(

                    $"F 失败：无法进入躲藏 → {hideSpotId}",

                    $"F failed: could not enter hide → {hideSpotId}"));

                return;

            }

        }



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



        LogDebug(BilingualDebug.Line(

            $"F 成功：躲藏 → {hideSpotId}（按 F 出箱，可反复躲藏）",

            $"F success: hiding → {hideSpotId} (press F to exit, can re-hide anytime)"));



        if (uiManager != null)

        {

            uiManager.ShowPrompt(BilingualDebug.Line(

                "已躲藏 - 按 F 出箱",

                "Hidden - Press F to exit"));

        }

    }



    public void ExitHide()

    {

        LogDebug(BilingualDebug.Line(

            "主动离开躲藏",

            "Exited hide manually"));



        if (playerController != null)

        {

            playerController.SetHidden(false);

            playerController.SetControllable(true);

        }



        if (coreFacade != null)

            coreFacade.ReportPlayerExitHiding();

        else

            hidingManager?.ReportPlayerExitHiding();



        animationController?.PlayHide(false);

        sfxController?.PlayHideExit();



        if (uiManager != null)

            uiManager.HidePrompt();

    }



    private void LogDebug(string message)

    {

        if (!logHideDebug)

            return;



        Debug.Log($"[PlayerHide] {message}", this);

    }

}

