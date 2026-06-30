using UnityEngine;

public class NpcStateMachine : MonoBehaviour
{
    [SerializeField] private NpcStatePolicy policy;

    private NpcController npcController;

    private NpcState? deferredState;

    private void Awake()
    {
        npcController = GetComponent<NpcController>();
    }

    /// <summary>
    /// Entry point. Called whenever any state flag changes.
    /// </summary>
    public void ResolveFlagChange(NpcStateSnapshot snapshot)
    {
        NpcState desiredState = EvaluateFlags(snapshot);
        desiredState = ApplyPriority(snapshot, desiredState);

        TryTransition(desiredState);
    }

    /// <summary>
    /// Decide what state the NPC wants to enter.
    /// No interrupt / busy logic here.
    /// </summary>
    private NpcState EvaluateFlags(NpcStateSnapshot s)
    {
        switch (s.currentState)
        {
            case NpcState.Idle:

                if (s.currentRageState == NpcRageState.Enraged)
                    return NpcState.Chase;

                return NpcState.Idle;


            case NpcState.Chase:

                if (s.currentIsTired)
                    return NpcState.Cooldown;

                if (!s.currentPlayerInView)
                    return NpcState.Search;

                if (s.currentPlayerInReach)
                    return NpcState.Dive;

                return NpcState.Chase;


            case NpcState.Search:

                if (s.currentPlayerInView)
                    return NpcState.Chase;

                return NpcState.Search;


            case NpcState.Dive:

                if (s.currentIsTired)
                    return NpcState.Cooldown;

                return NpcState.Dive;


            case NpcState.Cooldown:

                if (s.currentIsTired)
                    return NpcState.Cooldown;
                if (s.currentPlayerInReach)
                    return NpcState.Dive;
                return s.currentPlayerInView
                    ? NpcState.Chase
                    : NpcState.Search;


            case NpcState.Override:

                if (s.currentIsOverride)
                    return NpcState.Override;

                if (s.currentRageState == NpcRageState.Enraged)
                    return NpcState.Chase;
                else return NpcState.Idle;
        }

        return s.currentState;
    }

    /// <summary>
    /// Apply global priorities.
    /// Override is always highest.
    /// </summary>
    private NpcState ApplyPriority(NpcStateSnapshot s, NpcState desiredState)
    {
        if (s.currentIsOverride)
            return NpcState.Override;

        return desiredState;
    }

    /// <summary>
    /// Handles interrupt/defer policy.
    /// </summary>
    private void TryTransition(NpcState desiredState)
    {
        NpcState current = npcController.CurrentNpcState;

        if (current == desiredState)
            return;

        if (!CanInterrupt(current))
        {
            deferredState = desiredState;

            Debug.Log($"[NPC] {npcController.NpcId}: defer {current} -> {desiredState}");
            return;
        }

        Debug.Log($"[NPC] {npcController.NpcId}: {current} -> {desiredState}");

        npcController.SwitchNpcState(desiredState);
    }

    /// <summary>
    /// Called by behaviors when a non-interruptible state finishes.
    /// </summary>
    public void NotifyStateFinished()
    {
        if (!deferredState.HasValue)
            return;

        NpcState next = deferredState.Value;
        deferredState = null;

        Debug.Log($"[NPC] {npcController.NpcId}: resume deferred -> {next}");

        npcController.SwitchNpcState(next);
    }

    public void RequestTransition(NpcState requested)
    {
        TryTransition(requested);
    }

    /// <summary>
    /// Which states can be interrupted immediately?
    /// </summary>
    private bool CanInterrupt(NpcState state)
    {
        return !policy.nonInterruptible.Contains(state);
    }
}