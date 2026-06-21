public readonly struct StageContext
{
    public readonly string StageId;
    public readonly int TargetScore;
    public readonly float SurvivalTime;

    public StageContext(string stageId, int targetScore, float survivalTime)
    {
        StageId = stageId;
        TargetScore = targetScore;
        SurvivalTime = survivalTime;
    }
}
