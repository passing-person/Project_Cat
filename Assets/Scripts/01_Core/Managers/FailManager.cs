using UnityEngine;

public class FailManager : MonoBehaviour
{
    [Header("References")]
    public StageManager stageManager;
    public ObjectiveManager objectiveManager;

    [Header("Rules")]
    public CaughtRule caughtRule = CaughtRule.PendingDesignDecision;

    [Header("Debug")]
    [SerializeField] private bool hasFailed;

    public bool HasFailed => hasFailed;

    public void ResetFailState()
    {
        hasFailed = false;
    }

    public void ConfigureCaughtRule(CaughtRule rule)
    {
        caughtRule = rule;
    }

    public void HandlePlayerCaught()
    {
        if (hasFailed)
        {
            return;
        }

        switch (caughtRule)
        {
            case CaughtRule.AlwaysFail:
                FailStage(StageFailReason.CaughtByNpc);
                break;

            case CaughtRule.ClearIfEnoughScore:
                if (objectiveManager != null && objectiveManager.HasEnoughScore())
                {
                    if (stageManager != null)
                    {
                        stageManager.ClearStage();
                    }
                }
                else
                {
                    FailStage(StageFailReason.CaughtByNpc);
                }
                break;

            case CaughtRule.PendingDesignDecision:
            default:
                Debug.LogWarning("Caught rule is pending design confirmation. Defaulting to failure for safety.");
                FailStage(StageFailReason.CaughtByNpc);
                break;
        }
    }

    public void FailStage(StageFailReason reason)
    {
        if (hasFailed)
        {
            return;
        }

        hasFailed = true;
        string reasonText = reason.ToString();

        if (stageManager != null)
        {
            stageManager.FailStage(reasonText);
        }
    }

    public void FailStage(string reason)
    {
        if (hasFailed)
        {
            return;
        }

        hasFailed = true;

        if (stageManager != null)
        {
            stageManager.FailStage(reason);
        }
    }
}
