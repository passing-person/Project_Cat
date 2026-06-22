public enum GameState
{
    Boot,
    StageIntro,
    Playing,
    Chasing,
    Hidden,
    StageClear,
    StageFailed,
    GameOver,
    Ending
}

public enum MischiefType
{
    Stomp,
    Press,
    Push,
    Meow,
    Cute,
    Hide,
    Custom
}

public enum NpcType
{
    Supervisor,
    Worker,
    Cleaner,
    Security,
    Special
}

public enum NpcRageState
{
    Calm,
    Annoyed,
    Angry,
    Enraged
}

public enum ObjectiveType
{
    ReachScore,
    EnrageSupervisor,
    SurviveChase,
    InteractWithTarget,
    Custom
}

public enum StageFailReason
{
    None,
    CaughtByNpc,
    CaughtBySecurity,
    OutOfBounds,
    TimeOver,
    Custom
}

public enum CaughtRule
{
    PendingDesignDecision,
    AlwaysFail,
    ClearIfEnoughScore
}
