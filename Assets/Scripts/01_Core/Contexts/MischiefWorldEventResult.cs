using UnityEngine;

public readonly struct MischiefWorldEventResult
{
    public readonly bool Dispatched;
    public readonly string TargetId;
    public readonly MischiefWorldEventType EventType;
    public readonly string AssignedNpcId;
    public readonly Vector3 EventPosition;
    public readonly int DispatchedCount;
    public readonly string Message;

    public MischiefWorldEventResult(
        bool dispatched,
        string targetId,
        MischiefWorldEventType eventType,
        string assignedNpcId,
        Vector3 eventPosition,
        int dispatchedCount,
        string message)
    {
        Dispatched = dispatched;
        TargetId = targetId ?? string.Empty;
        EventType = eventType;
        AssignedNpcId = assignedNpcId ?? string.Empty;
        EventPosition = eventPosition;
        DispatchedCount = Mathf.Max(0, dispatchedCount);
        Message = message ?? string.Empty;
    }

    public static MischiefWorldEventResult Ignored(string targetId, MischiefWorldEventType eventType, Vector3 position, string message)
    {
        return new MischiefWorldEventResult(false, targetId, eventType, string.Empty, position, 0, message);
    }

    public static MischiefWorldEventResult Routed(string targetId, MischiefWorldEventType eventType, string assignedNpcId, Vector3 position, int dispatchedCount)
    {
        string label = string.IsNullOrEmpty(assignedNpcId) ? "NPC" : assignedNpcId;
        return new MischiefWorldEventResult(true, targetId, eventType, assignedNpcId, position, dispatchedCount, "Event routed to " + label);
    }
}
