using UnityEngine;

public class NpcStateMachine : MonoBehaviour
{
    [SerializeField] private NpcStatePolicy policy;

    private NpcController controller;
    private NpcView view;

    private NpcState? deferredState;

    private bool IsSecurity => controller.IsSecurity;

    private void Awake()
    {
        LazyInstantiate();
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
        if (IsSecurity)
            return EvaluateSecurityFlags(s);

        switch (s.currentState)
        {
            case NpcState.Idle:

                if (s.currentRageState == NpcRageState.Enraged)
                    return NpcState.Chase;

                return NpcState.Idle;


            case NpcState.Chase:

                if (s.currentIsTired)
                    return NpcState.Cooldown;

                if (s.currentRageState != NpcRageState.Enraged)
                    return NpcState.Idle;

                // PlayerInReach is already validated by NpcView:
                // normal dive requires targeting interval + space rect;
                // close-range dive bypasses the space rect and is handled as a warp dive.
                if (s.currentPlayerInReach && view != null 
                    && view.DiveRequestIsValid && !view.PlayerHidden)
                    return NpcState.Dive;

                // PlayerInView means the NPC has an actual or snapshot target.
                // Once no target knowledge remains, Chase falls into Search.
                if (!s.currentPlayerInView)
                    return NpcState.Search;

                return NpcState.Chase;


            case NpcState.Search:

                if (s.currentPlayerInReach && view != null 
                    && view.DiveRequestIsValid && !view.PlayerHidden)
                    return NpcState.Dive;

                // Search should resume Chase only on actual sector reacquire.
                // Snapshot retry after search timeout is owned by NpcSearchBehavior.
                if (view != null && view.PlayerInActualView)
                    return NpcState.Chase;

                return NpcState.Search;


            case NpcState.Dive:

                if (s.currentIsTired)
                    return NpcState.Cooldown;

                return NpcState.Dive;


            case NpcState.Cooldown:

                if (s.currentRageState != NpcRageState.Enraged)
                    return NpcState.Idle;

                if (s.currentIsTired)
                    return NpcState.Cooldown;

                if (s.currentPlayerInReach && view != null && view.DiveRequestIsValid)
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

    private NpcState EvaluateSecurityFlags(NpcStateSnapshot s)
    {
        switch (s.currentState)
        {
            case NpcState.Chase:
                if (s.currentIsTired) return NpcState.Cooldown;
                if (s.currentPlayerInReach) return NpcState.Dive;
                return NpcState.Chase;

            case NpcState.Dive:
                if (s.currentIsTired)
                    return NpcState.Cooldown;
                return NpcState.Dive;

            case NpcState.Cooldown:
                if (s.currentIsTired)
                    return NpcState.Cooldown;

                if (s.currentPlayerInReach && view != null && view.DiveRequestIsValid)
                    return NpcState.Dive;

                return NpcState.Chase;

            default: return NpcState.Chase;
        }
    }

    /// <summary>
    /// Apply global priorities.
    /// Override is always highest.
    /// </summary>
    private NpcState ApplyPriority(NpcStateSnapshot s, NpcState desiredState)
    {
        if (s.currentIsOverride) return NpcState.Chase;

        if (s.currentIsOverride)
            return NpcState.Override;

        return desiredState;
    }

    /// <summary>
    /// Handles interrupt/defer policy.
    /// </summary>
    private void TryTransition(NpcState desiredState)
    {
        NpcState current = controller.CurrentNpcState;

        if (current == desiredState)
            return;

        if (!CanInterrupt(current))
        {
            deferredState = desiredState;

            Debug.Log($"[NPC] {controller.NpcId}: defer {current} -> {desiredState}");
            return;
        }

        Debug.Log($"[NPC] {controller.NpcId}: {current} -> {desiredState}");

        controller.SwitchNpcState(desiredState);
    }

    public void NotifyStateFinished()
    {
        if (!deferredState.HasValue)
            return;

        NpcState next = deferredState.Value;
        deferredState = null;

        Debug.Log($"[NPC] {controller.NpcId}: resume deferred -> {next}");

        controller.SwitchNpcState(next);
    }

    /// <summary>
    /// Called by behaviors when a non-interruptible state finishes.
    /// If a deferred state exists, resume it.
    /// Otherwise, go to fallbackState.
    /// This bypasses CanInterrupt because the current non-interruptible state has finished.
    /// </summary>
    public void NotifyStateFinished(NpcState fallbackState)
    {
        NpcState next;

        if (deferredState.HasValue)
        {
            next = deferredState.Value;
            deferredState = null;

            Debug.Log($"[NPC] {controller.NpcId}: resume deferred -> {next}");
        }
        else
        {
            next = fallbackState;

            Debug.Log($"[NPC] {controller.NpcId}: no deferred state, fallback -> {next}");
        }

        controller.SwitchNpcState(next);
    }

    public void ReEvaluateState()
    {
        controller.OnSnapshotRequest();
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
        return policy == null || !policy.nonInterruptible.Contains(state);
    }

    private void LazyInstantiate()
    {
        if (controller == null)
            controller = GetComponent<NpcController>();
        if (view == null)
            view = GetComponent<NpcView>();
    }
}
