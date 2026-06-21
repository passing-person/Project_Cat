using UnityEngine;

public readonly struct MischiefContext
{
    public readonly string ActorId;
    public readonly string TargetId;
    public readonly MischiefType MischiefType;

    // This is optional. The current GDD leans toward time-based scoring,
    // so most normal targets can keep this value at 0.
    public readonly int BaseScore;

    public readonly float BaseRageAmount;
    public readonly Vector3 Position;
    public readonly float RageRadius;

    // Optional owner/main target NPC. Empty means range-only targeting.
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
