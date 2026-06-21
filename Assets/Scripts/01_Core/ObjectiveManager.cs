using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private StageManager stageManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private UIManager uiManager;

    private StageData currentStageData;
    private float survivalTimer;
    private bool survivalTimerRunning;

    public void InitializeObjective(StageData stageData)
    {
        currentStageData = stageData;
        survivalTimer = 0f;
        survivalTimerRunning = false;

        if (uiManager != null)
            uiManager.SetObjectiveText(GetCurrentObjectiveText());
    }

    public void UpdateObjectiveProgress()
    {
        if (currentStageData == null)
            return;

        if (IsObjectiveComplete() && stageManager != null)
            stageManager.ClearStage();
    }

    public void StartSurvivalTimer()
    {
        survivalTimer = 0f;
        survivalTimerRunning = true;
    }

    private void Update()
    {
        if (!survivalTimerRunning || currentStageData == null)
            return;

        survivalTimer += Time.deltaTime;
        float remaining = Mathf.Max(0f, currentStageData.survivalTime - survivalTimer);

        if (uiManager != null)
            uiManager.SetTimer(remaining);

        if (survivalTimer >= currentStageData.survivalTime)
        {
            survivalTimerRunning = false;

            if (stageManager != null)
                stageManager.ClearStage();
        }
    }

    public bool IsObjectiveComplete()
    {
        if (currentStageData == null)
            return false;

        switch (currentStageData.objectiveType)
        {
            case ObjectiveType.ReachScore:
                return HasEnoughScore();

            case ObjectiveType.SurviveChase:
                return false;

            default:
                return false;
        }
    }

    public bool HasEnoughScore()
    {
        return scoreManager != null && scoreManager.HasReachedTargetScore();
    }

    public string GetCurrentObjectiveText()
    {
        if (currentStageData == null)
            return "";

        switch (currentStageData.objectiveType)
        {
            case ObjectiveType.ReachScore:
                return "Earn enough mischief score.";

            case ObjectiveType.SurviveChase:
                return "Annoy the supervisor, then survive the chase.";

            case ObjectiveType.EnrageSupervisor:
                return "Raise supervisor rage to 100%.";

            default:
                return "Complete the objective.";
        }
    }

    public bool ClearWhenCaughtAfterEnoughScore()
    {
        return currentStageData != null && currentStageData.clearWhenCaughtAfterEnoughScore;
    }
}
