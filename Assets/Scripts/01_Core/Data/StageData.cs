using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/Core/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage")]
    public string stageId = "Office";
    public string nextStageId = "";

    [Header("Objective")]
    public ObjectiveType objectiveType = ObjectiveType.SurviveChase;
    public int targetScore = 300;
    public float survivalTime = 30f;
    public CaughtRule caughtRule = CaughtRule.PendingDesignDecision;

    [Header("Score")]
    public float baseScoreRate = 10f;
    public float maxScoreMultiplierBonus = 12f;
    public float securityMultiplierOverride = 13f;

    public void NormalizeValues()
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            stageId = "Stage";
        }

        targetScore = Mathf.Max(0, targetScore);
        survivalTime = Mathf.Max(0f, survivalTime);
        baseScoreRate = Mathf.Max(0f, baseScoreRate);
        maxScoreMultiplierBonus = Mathf.Max(0f, maxScoreMultiplierBonus);
        securityMultiplierOverride = Mathf.Max(0f, securityMultiplierOverride);
    }

    public bool IsValid(out string message)
    {
        if (string.IsNullOrWhiteSpace(stageId))
        {
            message = "StageData.stageId is empty.";
            return false;
        }

        if (targetScore < 0)
        {
            message = "StageData.targetScore cannot be negative.";
            return false;
        }

        if (survivalTime < 0f)
        {
            message = "StageData.survivalTime cannot be negative.";
            return false;
        }

        if (baseScoreRate < 0f)
        {
            message = "StageData.baseScoreRate cannot be negative.";
            return false;
        }

        if (maxScoreMultiplierBonus < 0f)
        {
            message = "StageData.maxScoreMultiplierBonus cannot be negative.";
            return false;
        }

        message = "StageData is valid.";
        return true;
    }

    private void OnValidate()
    {
        NormalizeValues();
    }
}
