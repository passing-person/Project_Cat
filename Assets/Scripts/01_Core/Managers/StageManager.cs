using UnityEngine;

public class StageManager : MonoBehaviour
{
    [Header("References")]
    public GameManager gameManager;
    public ScoreManager scoreManager;
    public ObjectiveManager objectiveManager;
    public FailManager failManager;
    public MonoBehaviour uiBridgeBehaviour;

    [Header("Stage")]
    public StageData defaultStageData;

    [Header("Debug")]
    [SerializeField] private StageData currentStageData;
    [SerializeField] private string currentStageId;
    [SerializeField] private bool stageCleared;
    [SerializeField] private bool stageFailed;
    [SerializeField] private string lastFailReason;

    private ICoreUIBridge uiBridge;

    public string CurrentStageId => currentStageId;
    public StageData CurrentStageData => currentStageData;
    public bool StageCleared => stageCleared;
    public bool StageFailed => stageFailed;
    public string LastFailReason => lastFailReason;
    public bool IsStageFinished => stageCleared || stageFailed;

    private void Awake()
    {
        ResolveUIBridge();
    }

    public void LoadStage(StageData stageData)
    {
        currentStageData = stageData != null ? stageData : defaultStageData;

        if (currentStageData == null)
        {
            Debug.LogWarning("StageManager.LoadStage failed: no StageData is assigned.");
            return;
        }

        currentStageData.NormalizeValues();
        if (!currentStageData.IsValid(out string validationMessage))
        {
            Debug.LogWarning(validationMessage);
        }

        currentStageId = currentStageData.stageId;
        stageCleared = false;
        stageFailed = false;
        lastFailReason = string.Empty;

        if (scoreManager != null)
        {
            scoreManager.InitializeScore(
                currentStageData.targetScore,
                currentStageData.baseScoreRate,
                currentStageData.maxScoreMultiplierBonus);
        }

        if (objectiveManager != null)
        {
            objectiveManager.InitializeObjective(currentStageData);
        }

        if (failManager != null)
        {
            failManager.ResetFailState();
            failManager.ConfigureCaughtRule(currentStageData.caughtRule);
        }
    }

    public void StartStage()
    {
        if (currentStageData == null)
        {
            LoadStage(defaultStageData);
        }

        if (gameManager != null)
        {
            gameManager.SetGameState(GameState.Playing);
        }
    }

    public void ClearStage()
    {
        if (stageCleared)
        {
            return;
        }

        stageCleared = true;
        stageFailed = false;
        lastFailReason = string.Empty;

        if (gameManager != null)
        {
            gameManager.SetGameState(GameState.StageClear);
            gameManager.OnStageCleared(currentStageId);
        }

        if (uiBridge == null)
        {
            ResolveUIBridge();
        }

        if (uiBridge != null)
        {
            uiBridge.ShowStageClear();
        }

        Debug.Log($"Stage cleared: {currentStageId}");
    }

    public void FailStage(string reason)
    {
        if (stageCleared || stageFailed)
        {
            return;
        }

        stageFailed = true;
        stageCleared = false;
        lastFailReason = reason;

        if (gameManager != null)
        {
            gameManager.SetGameState(GameState.StageFailed);
            gameManager.OnStageFailed(currentStageId, reason);
        }

        if (uiBridge == null)
        {
            ResolveUIBridge();
        }

        if (uiBridge != null)
        {
            uiBridge.ShowStageFailed(reason);
        }

        Debug.Log($"Stage failed: {currentStageId}, reason: {reason}");
    }

    public bool IsStageGoalReached()
    {
        return objectiveManager != null && objectiveManager.IsObjectiveComplete();
    }

    public StageContext GetStageContext()
    {
        if (currentStageData == null)
        {
            return new StageContext(string.Empty, 0, 0f);
        }

        return new StageContext(
            currentStageData.stageId,
            currentStageData.targetScore,
            currentStageData.survivalTime);
    }

    private void ResolveUIBridge()
    {
        uiBridge = uiBridgeBehaviour as ICoreUIBridge;
    }
}
