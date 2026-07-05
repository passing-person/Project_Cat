using UnityEngine;

public readonly struct MischiefWorldEventContext
{
    public readonly string ActorId;
    public readonly string TargetId;
    public readonly MischiefWorldEventType EventType;
    public readonly Vector3 Position;
    public readonly MischiefWorldEventResolveMode ResolveMode;
    public readonly string PreferredNpcId;
    public readonly NpcType PreferredNpcType;
    public readonly bool DisableTargetAfterResponse;

    public MischiefWorldEventContext(
        string actorId,
        string targetId,
        MischiefWorldEventType eventType,
        Vector3 position,
        MischiefWorldEventResolveMode resolveMode = MischiefWorldEventResolveMode.None,
        string preferredNpcId = "",
        NpcType preferredNpcType = NpcType.Special,
        bool disableTargetAfterResponse = false)
    {
        ActorId = string.IsNullOrWhiteSpace(actorId) ? "Player" : actorId;
        TargetId = string.IsNullOrWhiteSpace(targetId) ? "UnknownTarget" : targetId;
        EventType = eventType;
        Position = position;
        ResolveMode = resolveMode;
        PreferredNpcId = preferredNpcId ?? string.Empty;
        PreferredNpcType = preferredNpcType;
        DisableTargetAfterResponse = disableTargetAfterResponse;
    }

    public MischiefWorldEventContext WithEventType(MischiefWorldEventType eventType)
    {
        return new MischiefWorldEventContext(ActorId, TargetId, eventType, Position, ResolveMode, PreferredNpcId, PreferredNpcType, DisableTargetAfterResponse);
    }

    public MischiefWorldEventContext WithResolveMode(MischiefWorldEventResolveMode resolveMode)
    {
        return new MischiefWorldEventContext(ActorId, TargetId, EventType, Position, resolveMode, PreferredNpcId, PreferredNpcType, DisableTargetAfterResponse);
    }

    public static MischiefWorldEventType InferEventType(string targetId, MischiefType mischiefType)
    {
        string normalized = string.IsNullOrEmpty(targetId) ? string.Empty : targetId.ToLowerInvariant();

        if (normalized.Contains("light") || normalized.Contains("lamp") || normalized.Contains("switch"))
        {
            return MischiefWorldEventType.LightToggle;
        }

        if (normalized.Contains("printer") || normalized.Contains("print"))
        {
            return MischiefWorldEventType.PrinterMess;
        }

        if (normalized.Contains("water") || normalized.Contains("dispenser"))
        {
            return MischiefWorldEventType.WaterDispenserMess;
        }

        if (normalized.Contains("mess") || normalized.Contains("spill") || normalized.Contains("trash"))
        {
            return MischiefWorldEventType.GenericMess;
        }

        return MischiefWorldEventType.None;
    }

    public static MischiefWorldEventResolveMode GetDefaultResolveMode(MischiefWorldEventType eventType)
    {
        switch (eventType)
        {
            case MischiefWorldEventType.LightToggle:
                return MischiefWorldEventResolveMode.NearestNpc;
            case MischiefWorldEventType.PrinterMess:
            case MischiefWorldEventType.WaterDispenserMess:
            case MischiefWorldEventType.GenericMess:
                return MischiefWorldEventResolveMode.NearestCleaner;
            default:
                return MischiefWorldEventResolveMode.None;
        }
    }

    public static bool ShouldDisableTargetAfterResponse(MischiefWorldEventType eventType)
    {
        return eventType == MischiefWorldEventType.PrinterMess
            || eventType == MischiefWorldEventType.WaterDispenserMess
            || eventType == MischiefWorldEventType.GenericMess;
    }
}
