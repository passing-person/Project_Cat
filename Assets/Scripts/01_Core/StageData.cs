using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/Stage Data")]
public class StageData : ScriptableObject
{
    [Header("Stage")]
    public string stageId = "SupervisorOffice";
    public string nextStageId = "";

    [Header("Objective")]
    public ObjectiveType objectiveType = ObjectiveType.SurviveChase;
    public int targetScore = 1000;
    public float survivalTime = 30f;

    [Header("Score")]
    public float baseScoreRate = 10f;
    public float maxScoreMultiplierBonus = 12f;

    [Header("Temporary Rule Switches")]
    public bool clearWhenCaughtAfterEnoughScore = false;
}
