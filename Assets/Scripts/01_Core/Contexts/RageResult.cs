public readonly struct RageResult
{
    public readonly string NpcId;
    public readonly float PreviousRage;
    public readonly float CurrentRage;
    public readonly NpcRageState PreviousState;
    public readonly NpcRageState CurrentState;
    public readonly bool StateChanged;
    public readonly bool ReachedMaxRage;

    public RageResult(
        string npcId,
        float previousRage,
        float currentRage,
        NpcRageState previousState,
        NpcRageState currentState,
        bool reachedMaxRage)
    {
        NpcId = npcId;
        PreviousRage = previousRage;
        CurrentRage = currentRage;
        PreviousState = previousState;
        CurrentState = currentState;
        StateChanged = previousState != currentState;
        ReachedMaxRage = reachedMaxRage;
    }
}
