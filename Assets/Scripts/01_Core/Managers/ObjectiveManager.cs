using UnityEngine;

public class ObjectiveManager : MonoBehaviour
{
    [Header("References")]
    public ScoreManager scoreManager;
    public StageManager stageManager;
    public MonoBehaviour uiBridgeBehaviour;

    [Header("Debug")]
    [SerializeField] private ObjectiveType objectiveType = ObjectiveType.SurviveChase;
    [SerializeField] private float survivalTime = 30f;
    [SerializeField] private float remainingSurvivalTime;
    [SerializeField] private bool survivalTimerRunning;

    private ICoreUIBridge uiBridge;

    public ObjectiveType CurrentObjectiveType => objectiveType;
    public float RemainingSurvivalTime => remainingSurvivalTime;
    public bool IsSurvivalTimerRunning => survivalTimerRunning;

    private void Awake()
    {
        ResolveUIBridge();
    }

    private void Update()
    {
        TickSurvivalTimer(Time.deltaTime);

        if (objectiveType == ObjectiveType.ReachScore && HasEnoughScore() && stageManager != null)
        {
            stageManager.ClearStage();
        }
    }

    public void InitializeObjective(StageData stageData)
    {
        if (stageData == null)
        {
            objectiveType = ObjectiveType.SurviveChase;
            survivalTime = 30f;
        }
        else
        {
            objectiveType = stageData.objectiveType;
            survivalTime = Mathf.Max(0f, stageData.survivalTime);
        }

        remainingSurvivalTime = survivalTime;
        survivalTimerRunning = false;
        RefreshObjectiveUI();
        RefreshTimerUI();
    }

    public void UpdateObjectiveProgress()
    {
        RefreshObjectiveUI();

        if (objectiveType == ObjectiveType.ReachScore && HasEnoughScore() && stageManager != null)
        {
            stageManager.ClearStage();
        }
    }

    public void StartSurvivalTimer()
    {
        remainingSurvivalTime = survivalTime;
        survivalTimerRunning = true;
        RefreshTimerUI();
    }

    public void StopSurvivalTimer()
    {
        survivalTimerRunning = false;
    }

    public void TickSurvivalTimer(float deltaTime)
    {
        if (!survivalTimerRunning || deltaTime <= 0f)
        {
            return;
        }

        remainingSurvivalTime = Mathf.Max(0f, remainingSurvivalTime - deltaTime);
        RefreshTimerUI();

        if (remainingSurvivalTime <= 0f)
        {
            survivalTimerRunning = false;
            if (stageManager != null)
            {
                stageManager.ClearStage();
            }
        }
    }

    public bool IsObjectiveComplete()
    {
        switch (objectiveType)
        {
            case ObjectiveType.ReachScore:
                return HasEnoughScore();
            case ObjectiveType.SurviveChase:
                return remainingSurvivalTime <= 0f;
            default:
                return false;
        }
    }

    public bool HasEnoughScore()
    {
        return scoreManager != null && scoreManager.HasReachedTargetScore();
    }

    public void ForceCompleteIfScoreReached()
    {
        if (HasEnoughScore() && stageManager != null)
        {
            stageManager.ClearStage();
        }
    }

    public string GetCurrentObjectiveText()
    {
        switch (objectiveType)
        {
            case ObjectiveType.ReachScore:
                return "Earn enough mischief score.";
            case ObjectiveType.SurviveChase:
                return "Survive the chase.";
            case ObjectiveType.EnrageSupervisor:
                return "Make the supervisor angry.";
            case ObjectiveType.InteractWithTarget:
                return "Interact with the target.";
            default:
                return "Cause mischief.";
        }
    }

    private void RefreshObjectiveUI()
    {
        if (uiBridge == null)
        {
            ResolveUIBridge();
        }

        if (uiBridge != null)
        {
            uiBridge.SetObjectiveText(GetCurrentObjectiveText());
        }
    }

    private void RefreshTimerUI()
    {
        if (uiBridge == null)
        {
            ResolveUIBridge();
        }

        if (uiBridge != null)
        {
            uiBridge.SetTimer(remainingSurvivalTime);
        }
    }

    private void ResolveUIBridge()
    {
        uiBridge = uiBridgeBehaviour as ICoreUIBridge;
    }
}
