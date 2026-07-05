public interface IMischiefWorldEventTarget
{
    string WorldEventTargetId { get; }
    MischiefWorldEventType WorldEventType { get; }
    bool CanStartWorldEvent();
    string GetUnavailableReason();
    void OnWorldEventStarted(MischiefWorldEventContext context);
    void OnWorldEventCompleted(bool disableTarget, float cooldownDuration);
}
