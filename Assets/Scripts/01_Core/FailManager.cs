using UnityEngine;

public class FailManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private StageManager stageManager;
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private UIManager uiManager;

    private bool resultHandled;

    public void ResetFailState()
    {
        resultHandled = false;
    }

    public void HandlePlayerCaught()
    {
        if (resultHandled)
            return;

        // This supports the design question: caught after enough score may clear the stage.
        if (objectiveManager != null
            && objectiveManager.ClearWhenCaughtAfterEnoughScore()
            && objectiveManager.HasEnoughScore())
        {
            resultHandled = true;

            if (stageManager != null)
                stageManager.ClearStage();

            return;
        }

        FailStage(StageFailReason.CaughtByNpc);
    }

    public void FailStage(StageFailReason reason)
    {
        FailStage(reason.ToString());
    }

    public void FailStage(string reason)
    {
        if (resultHandled)
            return;

        resultHandled = true;

        if (uiManager != null)
            uiManager.ShowStageFailed(reason);

        if (stageManager != null)
            stageManager.FailStage(reason);
    }
}
