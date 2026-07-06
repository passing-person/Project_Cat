using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class NpcOverrideBehavior : MonoBehaviour
{
    [Header("Override Schedule")]
    [SerializeField] private List<OverrideScheduleEntry> OverrideSchedule = new();

    [Header("Generic Light Reaction")]
    [SerializeField, Min(0f)] private float lightStunDuration = 1.25f;

    [Header("Reactor Movement")]
    [SerializeField] private bool useChaseSpeedForOverrideMove = false;

    private NpcController controller;
    private NpcNavigate nav;
    private NpcAnimationMachine anim;
    private CoreFacade facade;

    private readonly List<OverrideJob> pendingJobs = new();
    private Coroutine processRoutine;
    private OverrideJob currentJob;
    private long nextSequence;

    private string NpcId => controller != null ? controller.NpcId : gameObject.name;
    private NpcType NpcType => controller != null ? controller.NpcType : NpcType.Worker;

    private void Awake()
    {
        LazyInstantiate();
    }

    // These are called by NpcController when the gameplay state becomes Override.
    // They are not event entry points; they only begin queue processing for this NPC type.
    public void Supervisor() => EnsureProcessing();
    public void Worker() => EnsureProcessing();
    public void Cleaner() => EnsureProcessing();
    public void Security() => EnsureProcessing();

    public void ExitState()
    {
        if (processRoutine != null)
        {
            StopCoroutine(processRoutine);
            processRoutine = null;
        }

        currentJob = null;

        if (nav != null)
            nav.StopNav();
    }

    public void OnMischiefWorldEvent(MischiefWorldEventContext context)
    {
        LazyInstantiate();

        bool enqueued = false;

        switch (context.EventType)
        {
            case MischiefWorldEventType.LightToggle:
                // Generic light reaction applies to every NPC that receives the global broadcast.
                EnqueueJob(context, OverrideReactionRole.Generic);
                enqueued = true;

                // Core guarantees only one NPC receives ShouldReact == true.
                if (context.ShouldReact)
                {
                    EnqueueJob(context, OverrideReactionRole.Reactor);
                    enqueued = true;
                }
                break;

            case MischiefWorldEventType.PrinterMess:
            case MischiefWorldEventType.WaterDispenserMess:
            case MischiefWorldEventType.GenericMess:
                // Mess has no generic reaction. Only the assigned reactor handles it.
                if (context.ShouldReact)
                {
                    EnqueueJob(context, OverrideReactionRole.Reactor);
                    enqueued = true;
                }
                break;

            case MischiefWorldEventType.MicrophoneBroadcast:
                // This event is handled elsewhere as generic rage/audio behavior.
                return;

            case MischiefWorldEventType.Auto:
                // CoreFacade normally resolves Auto before dispatching to NPCs. If it reaches here,
                // the caller bypassed normal routing or Core could not infer a concrete event type.
                Debug.LogWarning($"[NPC Override] {NpcId}: received unresolved Auto world event for target '{context.TargetId}'. Ignored.");
                return;

            case MischiefWorldEventType.None:
                Debug.LogWarning($"[NPC Override] {NpcId}: cannot resolve world event of type None for target '{context.TargetId}'.");
                return;

            default:
                Debug.LogWarning($"[NPC Override] {NpcId}: world event of type {context.EventType} is not implemented.");
                return;
        }

        if (!enqueued)
            return;

        SortPendingJobs();

        if (controller != null)
            controller.RequestOverrideState();

        EnsureProcessing();
    }

    private void EnqueueJob(MischiefWorldEventContext context, OverrideReactionRole role)
    {
        int priority = ResolvePriority(context.EventType, role, context.TargetId);

        pendingJobs.Add(new OverrideJob(
            context,
            role,
            priority,
            nextSequence++
        ));

        Debug.Log(
            $"[NPC Override] {NpcId}: queued {role} {context.EventType}. " +
            $"Target={context.TargetId}, Priority={priority}."
        );
    }

    private void EnsureProcessing()
    {
        LazyInstantiate();

        if (processRoutine != null)
            return;

        if (pendingJobs.Count <= 0)
        {
            if (controller != null && controller.CurrentNpcState == NpcState.Override)
                controller.FinishOverrideState();
            return;
        }

        processRoutine = StartCoroutine(ProcessOverrideQueueRoutine());
    }

    private IEnumerator ProcessOverrideQueueRoutine()
    {
        while (pendingJobs.Count > 0)
        {
            SortPendingJobs();

            currentJob = pendingJobs[0];
            pendingJobs.RemoveAt(0);

            Debug.Log(
                $"[NPC Override] {NpcId}: start {currentJob.Role} {currentJob.Context.EventType}. " +
                $"Target={currentJob.Context.TargetId}, Priority={currentJob.Priority}, Sequence={currentJob.Sequence}."
            );

            if (currentJob.Role == OverrideReactionRole.Generic)
            {
                yield return ExecuteGenericReaction(currentJob.Context);
            }
            else
            {
                yield return ExecuteReactorReaction(currentJob.Context);
            }

            currentJob = null;
            yield return null;
        }

        processRoutine = null;

        if (controller != null)
            controller.FinishOverrideState();
    }

    private IEnumerator ExecuteGenericReaction(MischiefWorldEventContext context)
    {
        switch (context.EventType)
        {
            case MischiefWorldEventType.LightToggle:
                yield return ExecuteGenericLightReaction(context);
                break;

            case MischiefWorldEventType.GenericMess:
            case MischiefWorldEventType.PrinterMess:
            case MischiefWorldEventType.WaterDispenserMess:
                yield return ExecuteGenericMessReaction(context);
                break;
        }
    }

    private IEnumerator ExecuteGenericLightReaction(MischiefWorldEventContext context)
    {
        // Abstraction layer for NPC-type-specific generic reactions.
        // Security can later override this to turn on a flashlight while other NPCs stun.
        switch (NpcType)
        {
            case NpcType.Security:
                yield return ExecuteSecurityLightReaction(context);
                break;

            default:
                yield return ExecuteNonSecurityLightStun(context);
                break;
        }
    }

    private IEnumerator ExecuteSecurityLightReaction(MischiefWorldEventContext context)
    {
        // TODO: replace this with Security flashlight behavior when that animation/tool exists.
        yield return ExecuteNonSecurityLightStun(context);
    }

    private IEnumerator ExecuteNonSecurityLightStun(MischiefWorldEventContext context)
    {
        if (nav != null)
        {
            nav.StopPatrol();
            nav.StopNav();
        }

        // placeholder for possible stunned animation.
        // no animation is played for now.
        // play suitable anim for current anchor mode
        if (anim.CurrentNpcAnchorMode == NpcAnchorMode.Sitting)
            anim.PlayIdleSitting();
        else if (anim.CurrentNpcAnchorMode == NpcAnchorMode.Standing)
            anim.PlayLocomotion(); // Idle standing clip
        else anim.PlayFallback();

        if (lightStunDuration > 0f)
        {
            Debug.Log($"[NPC Override] {NpcId}: stunned for {lightStunDuration} second(s).");
            yield return new WaitForSeconds(lightStunDuration);
        }
    }

    private IEnumerator ExecuteGenericMessReaction(MischiefWorldEventContext context)
    {
        yield return null;
    }

    private IEnumerator ExecuteReactorReaction(MischiefWorldEventContext context)
    {
        if (!context.ShouldReact)
        {
            Debug.LogWarning($"[NPC Override] {NpcId}: tried to execute reactor job with ShouldReact=false. Ignored.");
            yield break;
        }

        if (nav != null)
        {
            nav.StopPatrol();
            nav.StopNav();
        }

        if (anim != null)
            anim.PlayOverrideMove();

        bool arrived = false;
        void OnArrived() => arrived = true;

            
        if (nav == null) arrived = true;

        nav.DestinationReached += OnArrived;

        if (context.EventType == MischiefWorldEventType.GenericMess ||
            context.EventType == MischiefWorldEventType.PrinterMess ||
            context.EventType == MischiefWorldEventType.WaterDispenserMess)
        {
            NavigateToMess(context);
        }
        else if (context.EventType == MischiefWorldEventType.LightToggle)
        {
            nav.StartNavToPoint(context.Position, useChaseSpeedForOverrideMove);
        }

        while (!arrived)
        {
            yield return null;
        }

        nav.DestinationReached -= OnArrived;
        nav.StopNav();

        CompleteWorldEventForReactor(context);
    }

    private void NavigateToMess(MischiefWorldEventContext context)
    {
        if (NpcType != NpcType.Cleaner) return;

        if (TryGetWorldEventGameObject(context.TargetId, out var targetGO))
        {
            var script = targetGO.GetComponent<AdaptiveBlockLogic>();
            if (script == null)
                Debug.Log($"[NPC Override] {NpcId}: missing script \"AdaptiveBlockLogic\" on the target.");
            if (script.TryGetClosestFallbackPoint
                    (gameObject.transform.position, out var closest))
            {
                nav.StartNavToPointApprox(closest, useChaseSpeedForOverrideMove);
            }
            else
                Debug.Log($"[NPC Override] {NpcId}: transforms not assigned in script \"AdaptiveBlockLogic\" on the target.");
        }
    }

    private void CompleteWorldEventForReactor(MischiefWorldEventContext context)
    {
        if (!context.ShouldReact)
            return;

        if (string.IsNullOrWhiteSpace(context.TargetId))
            return;

        switch (context.EventType)
        {
            case MischiefWorldEventType.LightToggle:
                // Light is turned off and made usable again immediately.
                CompleteMischiefWorldEvent(context.TargetId, false, 0f);
                break;

            case MischiefWorldEventType.PrinterMess:
            case MischiefWorldEventType.WaterDispenserMess:
            case MischiefWorldEventType.GenericMess:
                // Mess targets are cleaned and disabled for this stage.
                CompleteMischiefWorldEvent(context.TargetId);
                break;
        }

        Debug.Log($"[NPC Override] {NpcId}: Reactor {context.ReactorNpcId} has concluded its reaction.");
    }

    private void CompleteMischiefWorldEvent(string targetId)
    {
        if (controller != null)
            controller.CompleteMischiefWorldEvent(targetId);
    }

    private void CompleteMischiefWorldEvent(string targetId, bool disableTarget, float cooldownDuration)
    {
        if (controller != null)
            controller.CompleteMischiefWorldEvent(targetId, disableTarget, cooldownDuration);
    }

    private int ResolvePriority(MischiefWorldEventType eventType, OverrideReactionRole role, string targetId)
    {
        int bestPriority = int.MaxValue;
        bool found = false;

        for (int i = 0; i < OverrideSchedule.Count; i++)
        {
            OverrideScheduleEntry entry = OverrideSchedule[i];
            if (!entry.Matches(eventType, role, targetId))
                continue;

            if (!found || entry.Priority < bestPriority)
            {
                found = true;
                bestPriority = entry.Priority;
            }
        }

        if (found)
            return bestPriority;

        return GetDefaultPriority(eventType, role);
    }

    private int GetDefaultPriority(MischiefWorldEventType eventType, OverrideReactionRole role)
    {
        // Smaller number = higher priority.
        // Default Light behavior: everyone stuns first; assigned reactor then goes to fix the light.
        if (eventType == MischiefWorldEventType.LightToggle && role == OverrideReactionRole.Generic)
            return 0;

        if (role == OverrideReactionRole.Reactor)
            return 10;

        return 100;
    }

    private void SortPendingJobs()
    {
        pendingJobs.Sort((a, b) =>
        {
            int priorityCompare = a.Priority.CompareTo(b.Priority);
            if (priorityCompare != 0)
                return priorityCompare;

            return a.Sequence.CompareTo(b.Sequence);
        });
    }

    private bool TryGetWorldEventGameObject(string targetId, out GameObject target)
    {
        if(facade.TryGetMischiefWorldEventTarget(targetId, out var script))
        {
            target = (script as MonoBehaviour).gameObject;
            return true;
        }
        target = null;
        return false;
    }

    private void LazyInstantiate()
    {
        if (controller == null)
            controller = GetComponent<NpcController>();

        if (nav == null)
            nav = GetComponent<NpcNavigate>();

        if (anim == null)
            anim = GetComponent<NpcAnimationMachine>();

        if (facade == null)
            facade = FindFirstObjectByType<CoreFacade>();
    }

    private enum OverrideReactionRole
    {
        Any,
        Generic,
        Reactor
    }

    private sealed class OverrideJob
    {
        public readonly MischiefWorldEventContext Context;
        public readonly OverrideReactionRole Role;
        public readonly int Priority;
        public readonly long Sequence;

        public OverrideJob(MischiefWorldEventContext context, OverrideReactionRole role, int priority, long sequence)
        {
            Context = context;
            Role = role;
            Priority = priority;
            Sequence = sequence;
        }
    }

    [System.Serializable]
    private struct OverrideScheduleEntry
    {
        [SerializeField] private MischiefWorldEventType OverrideType;
        [SerializeField] private OverrideReactionRole reactionRole;
        [Tooltip("Optional. Leave empty to match any target id.")]
        [SerializeField] private string targetId;
        [Tooltip("Smaller entries have higher priority.")]
        [SerializeField] private int priority;

        public readonly int Priority => priority;

        public bool Matches(MischiefWorldEventType eventType, OverrideReactionRole role, string eventTargetId)
        {
            if (OverrideType != eventType)
                return false;

            if (reactionRole != OverrideReactionRole.Any && reactionRole != role)
                return false;

            if (!string.IsNullOrWhiteSpace(targetId) && targetId != eventTargetId)
                return false;

            return true;
        }
    }

    [ContextMenu("Debug test World Event")]
    private void DebugtestWorldEvent()
    {
        GameObject go = new();
        go.name = "Debug World Event Object";
        go.transform.position = DebugPosition;
        go.AddComponent<AdaptiveBlockLogic>();
        MischiefWorldEventContext context = new(actorId,
            targetId,
            eventType,
            DebugPosition,
            resolveMode,
            preferredNpcId,
            preferredNpcType,
            disableTargetAfterResponse,
            shouldReact,
            DebugReactorNpcId);
        //facade.ReportMischiefWorldEvent(context);
        OnMischiefWorldEvent(context);
    }

    [Header("Debug params")]
    [SerializeField] string actorId = "Dummy Actor";
    [SerializeField] string targetId = "Dummy Target";
    [SerializeField] MischiefWorldEventType eventType;
    private Vector3 DebugPosition => debugTargetPoint.position;
    [SerializeField] Transform debugTargetPoint;
    [SerializeField] MischiefWorldEventResolveMode resolveMode;
    [SerializeField] string preferredNpcId;
    [SerializeField] NpcType preferredNpcType;
    [SerializeField] bool disableTargetAfterResponse = false;
    [SerializeField] bool shouldReact = false;
    private string DebugReactorNpcId => NpcId;
}
