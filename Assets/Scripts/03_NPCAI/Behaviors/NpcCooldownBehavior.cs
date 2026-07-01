using UnityEngine;

public class NpcCooldownBehavior : MonoBehaviour
{
    private NpcController controller;
    private NpcTimer timer;
    private NpcNavigate nav;
    private NpcStateMachine stateMachine;

    private NpcTimerType? activeCooldownTimer;

    public void Supervisor()
    {
        StartCooldown(InferSourceState());
    }

    public void Worker()
    {
        StartCooldown(InferSourceState());
    }

    public void Cleaner()
    {
        StartCooldown(InferSourceState());
    }

    public void Security()
    {
        StartCooldown(InferSourceState());
    }

    /// <summary>
    /// Preferred entry point.
    /// Call this from NpcController with the previous state.
    /// </summary>
    public void EnterFrom(NpcState sourceState)
    {
        StartCooldown(sourceState);
    }

    public void ExitState()
    {
        LazyInstantiate();

        StopCooldownTimer(NpcTimerType.ChaseCooldown);
        StopCooldownTimer(NpcTimerType.DiveCooldown);

        activeCooldownTimer = null;
    }

    private void StartCooldown(NpcState sourceState)
    {
        LazyInstantiate();

        // Cooldown means NPC is panting / recovering, so stop movement.
        nav.ToggleChasePlayer(false);

        // Prevent stale callbacks from a previous cooldown type.
        StopCooldownTimer(NpcTimerType.ChaseCooldown);
        StopCooldownTimer(NpcTimerType.DiveCooldown);

        activeCooldownTimer = ResolveCooldownTimerType(sourceState);

        timer.StartTimer(activeCooldownTimer.Value, ResolveCooldownFinished);

        Debug.Log($"[NPC] {controller.NpcId}: Cooldown started from {sourceState}, using {activeCooldownTimer.Value}.");
    }

    private NpcTimerType ResolveCooldownTimerType(NpcState sourceState)
    {
        switch (sourceState)
        {
            case NpcState.Dive:
                return NpcTimerType.DiveCooldown;

            case NpcState.Chase:
                return NpcTimerType.ChaseCooldown;

            default:
                Debug.LogWarning(
                    $"[NPC] {controller.NpcId}: Cooldown entered from unexpected state {sourceState}. " +
                    $"Defaulting to ChaseCooldown."
                );
                return NpcTimerType.ChaseCooldown;
        }
    }

    private void ResolveCooldownFinished()
    {
        LazyInstantiate();

        if (controller.CurrentNpcState != NpcState.Cooldown)
            return;

        Debug.Log($"[NPC] {controller.NpcId}: Cooldown finished.");

        activeCooldownTimer = null;

        // Let the state machine decide the next valid state:
        // Chase / Dive / Search / Override depending on current flags.
        stateMachine.ReEvaluateState();
        stateMachine.NotifyStateFinished();
    }

    private void StopCooldownTimer(NpcTimerType timerType)
    {
        if (timer == null)
            return;

        timer.StopTimer(timerType);
        timer.ResetTimer(timerType);
    }

    /// <summary>
    /// Fallback for current delegate-style behavior calls.
    /// In the current NpcController setter, CurrentNpcState is still the previous state
    /// while the new state's enter behavior is being called.
    /// </summary>
    private NpcState InferSourceState()
    {
        LazyInstantiate();
        return controller.CurrentNpcState;
    }

    private void LazyInstantiate()
    {
        if (controller == null)
            controller = GetComponent<NpcController>();

        if (timer == null)
            timer = GetComponent<NpcTimer>();

        if (nav == null)
            nav = GetComponent<NpcNavigate>();

        if (stateMachine == null)
            stateMachine = GetComponent<NpcStateMachine>();
    }
}