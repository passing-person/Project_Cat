using UnityEngine;

public readonly struct MischiefContext
{
    public readonly string ActorId;
    public readonly string TargetId;
    public readonly MischiefType MischiefType;
    public readonly int BaseScore;
    public readonly float BaseRageAmount;
    public readonly Vector3 Position;
    public readonly float RageRadius;
    public readonly string PrimaryNpcId;

    public MischiefContext(
        string actorId,
        string targetId,
        MischiefType mischiefType,
        int baseScore,
        float baseRageAmount,
        Vector3 position,
        float rageRadius,
        string primaryNpcId = "")
    {
        ActorId = actorId;
        TargetId = targetId;
        MischiefType = mischiefType;
        BaseScore = baseScore;
        BaseRageAmount = baseRageAmount;
        Position = position;
        RageRadius = rageRadius;
        PrimaryNpcId = primaryNpcId;
    }
}
