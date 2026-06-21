using UnityEngine;

public class StageManager : MonoBehaviour
{
    public string CurrentStageId { get; private set; }
    public StageData CurrentStageData { get; private set; }

    [Header("Managers")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private FailManager failManager;
    [SerializeField] private UIManager uiManager;

    public void LoadStage(StageData stageData)
    {
        CurrentStageData = stageData;
        CurrentStageId = stageData != null ? stageData.stageId : "";

        if (scoreManager != null && stageData != null)
        {
            scoreManager.InitializeScore(stageData.targetScore);
            scoreManager.SetScoreConfig(stageData.baseScoreRate, stageData.maxScoreMultiplierBonus);
        }

        if (objectiveManager != null)
            objectiveManager.InitializeObjective(stageData);

        if (failManager != null)
            failManager.ResetFailState();
    }

    public void StartStage()
    {
        if (gameManager != null)
            gameManager.SetGameState(GameState.Playing);

        if (uiManager != null && objectiveManager != null)
            uiManager.SetObjectiveText(objectiveManager.GetCurrentObjectiveText());
    }

    public void ClearStage()
    {
        if (gameManager != null)
            gameManager.OnStageCleared(CurrentStageId);

        if (uiManager != null)
            uiManager.ShowStageClear();
    }

    public void FailStage(string reason)
    {
        if (gameManager != null)
            gameManager.OnStageFailed(CurrentStageId, reason);
    }

    public bool IsStageGoalReached()
    {
        return objectiveManager != null && objectiveManager.IsObjectiveComplete();
    }

    public StageContext GetStageContext()
    {
        if (CurrentStageData == null)
            return new StageContext("", 0, 0f);

        return new StageContext(CurrentStageData.stageId, CurrentStageData.targetScore, CurrentStageData.survivalTime);
    }
}
