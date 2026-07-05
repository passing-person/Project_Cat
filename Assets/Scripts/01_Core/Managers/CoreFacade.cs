using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class CoreFacade : MonoBehaviour
{
    [Header("Core Managers")]
    public GameManager gameManager;
    public StageManager stageManager;
    public MischiefManager mischiefManager;
    public ScoreManager scoreManager;
    public RageManager rageManager;
    public ObjectiveManager objectiveManager;
    public FailManager failManager;
    public HidingManager hidingManager;

    [Header("External Bridge")]
    public MonoBehaviour uiBridgeBehaviour;

    [Header("Cute Action")]
    public float cuteActionRadius = 5f;
    public float cuteActionRageReduction = 20f;

    [Header("Security")]
    public float defaultSecurityMultiplier = 13f;

    [Header("Options")]
    public bool autoResolveReferences = true;
    public bool autoWireReferences = true;

    private readonly Dictionary<string, IMischiefWorldEventTarget> worldEventTargets = new Dictionary<string, IMischiefWorldEventTarget>();
    private readonly Dictionary<string, bool> pendingWorldEventDisableByTargetId = new Dictionary<string, bool>();

    public bool HasValidCoreReferences => ValidateCoreReferences(out _);
    public GameState CurrentGameState => gameManager != null ? gameManager.CurrentState : GameState.Boot;
    public StageData CurrentStageData => stageManager != null ? stageManager.CurrentStageData : null;
    public string CurrentStageId => stageManager != null ? stageManager.CurrentStageId : string.Empty;
    public int CurrentScore => scoreManager != null ? scoreManager.CurrentScore : 0;
    public float CurrentScoreFloat => scoreManager != null ? scoreManager.CurrentScoreFloat : 0f;
    public float CurrentMultiplier => scoreManager != null ? scoreManager.CurrentMultiplier : 1f;
    public int TargetScore => scoreManager != null ? scoreManager.targetScore : 0;
    public bool HasEnoughScore => scoreManager != null && scoreManager.HasReachedTargetScore();
    public float RemainingHideTime => hidingManager != null ? hidingManager.RemainingHideTime : 0f;
    public string ActiveHideSpotId => hidingManager != null ? hidingManager.ActiveHideSpotId : string.Empty;
    public bool IsPlayerHidden => hidingManager != null && hidingManager.IsHidden;
    public bool IsStageFinished => stageManager != null && stageManager.IsStageFinished;
    public bool StageCleared => stageManager != null && stageManager.StageCleared;
    public bool StageFailed => stageManager != null && stageManager.StageFailed;

    private void Awake()
    {
        if (autoResolveReferences)
        {
            ResolveReferences();
        }

        if (autoWireReferences)
        {
            WireReferences();
        }
    }

    [ContextMenu("Resolve Core References")]
    public void ResolveReferences()
    {
        if (gameManager == null) gameManager = FindObjectOfType<GameManager>();
        if (stageManager == null) stageManager = FindObjectOfType<StageManager>();
        if (mischiefManager == null) mischiefManager = FindObjectOfType<MischiefManager>();
        if (scoreManager == null) scoreManager = FindObjectOfType<ScoreManager>();
        if (rageManager == null) rageManager = FindObjectOfType<RageManager>();
        if (objectiveManager == null) objectiveManager = FindObjectOfType<ObjectiveManager>();
        if (failManager == null) failManager = FindObjectOfType<FailManager>();
        if (hidingManager == null) hidingManager = FindObjectOfType<HidingManager>();
    }

    [ContextMenu("Wire Core References")]
    public void WireReferences()
    {
        if (gameManager != null)
        {
            gameManager.stageManager = stageManager;
            if (uiBridgeBehaviour != null) gameManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (stageManager != null)
        {
            stageManager.gameManager = gameManager;
            stageManager.scoreManager = scoreManager;
            stageManager.objectiveManager = objectiveManager;
            stageManager.failManager = failManager;
            if (uiBridgeBehaviour != null) stageManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (mischiefManager != null)
        {
            mischiefManager.rageManager = rageManager;
            mischiefManager.scoreManager = scoreManager;
            mischiefManager.objectiveManager = objectiveManager;
            if (uiBridgeBehaviour != null) mischiefManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (scoreManager != null && uiBridgeBehaviour != null)
        {
            scoreManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (rageManager != null)
        {
            rageManager.scoreManager = scoreManager;
            if (uiBridgeBehaviour != null) rageManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (objectiveManager != null)
        {
            objectiveManager.scoreManager = scoreManager;
            objectiveManager.stageManager = stageManager;
            if (uiBridgeBehaviour != null) objectiveManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        if (failManager != null)
        {
            failManager.stageManager = stageManager;
            failManager.objectiveManager = objectiveManager;
        }

        if (hidingManager != null)
        {
            hidingManager.scoreManager = scoreManager;
            if (uiBridgeBehaviour != null) hidingManager.uiBridgeBehaviour = uiBridgeBehaviour;
        }

        ICoreUIBridge bridge = uiBridgeBehaviour as ICoreUIBridge;
        if (bridge != null)
        {
            SetUIBridge(uiBridgeBehaviour);
        }
    }

    public void SetUIBridge(MonoBehaviour bridgeBehaviour)
    {
        uiBridgeBehaviour = bridgeBehaviour;

        if (gameManager != null) gameManager.uiBridgeBehaviour = bridgeBehaviour;
        if (stageManager != null) stageManager.uiBridgeBehaviour = bridgeBehaviour;
        if (mischiefManager != null) mischiefManager.uiBridgeBehaviour = bridgeBehaviour;
        if (objectiveManager != null) objectiveManager.uiBridgeBehaviour = bridgeBehaviour;
        if (hidingManager != null) hidingManager.uiBridgeBehaviour = bridgeBehaviour;

        ICoreUIBridge bridge = bridgeBehaviour as ICoreUIBridge;

        if (scoreManager != null)
        {
            scoreManager.uiBridgeBehaviour = bridgeBehaviour;
            scoreManager.SetUIBridge(bridge);
        }

        if (rageManager != null)
        {
            rageManager.uiBridgeBehaviour = bridgeBehaviour;
            rageManager.SetUIBridge(bridge);
        }

        if (hidingManager != null)
        {
            hidingManager.SetUIBridge(bridge);
        }
    }

    public void LoadStage(StageData stageData)
    {
        if (stageManager == null)
        {
            Debug.LogWarning("CoreFacade.LoadStage failed: StageManager is missing.");
            return;
        }

        stageManager.LoadStage(stageData);

        if (hidingManager != null)
        {
            hidingManager.ConfigureFromStageData(stageManager.CurrentStageData);
            hidingManager.ResetHidingState();
        }

        if (mischiefManager != null)
        {
            mischiefManager.ResetTargetStates();
        }
    }

    public void StartStage()
    {
        if (stageManager == null)
        {
            Debug.LogWarning("CoreFacade.StartStage failed: StageManager is missing.");
            return;
        }

        stageManager.StartStage();
    }

    public bool ApplyMischief(MischiefContext context)
    {
        if (mischiefManager == null)
        {
            Debug.LogWarning("CoreFacade.ApplyMischief failed: MischiefManager is missing.");
            return false;
        }

        return mischiefManager.ApplyMischief(context);
    }

    public bool CanApplyMischief(string targetId)
    {
        return mischiefManager != null && mischiefManager.CanApplyMischief(targetId);
    }

    public MischiefTargetState GetMischiefTargetState(string targetId)
    {
        return mischiefManager != null ? mischiefManager.GetMischiefTargetState(targetId) : MischiefTargetState.Disabled;
    }

    public void SetMischiefTargetState(string targetId, MischiefTargetState state)
    {
        if (mischiefManager == null)
        {
            Debug.LogWarning("CoreFacade.SetMischiefTargetState failed: MischiefManager is missing.");
            return;
        }

        mischiefManager.SetMischiefTargetState(targetId, state);
    }

    public void StartMischiefTargetCooldown(string targetId, float duration)
    {
        if (mischiefManager == null)
        {
            Debug.LogWarning("CoreFacade.StartMischiefTargetCooldown failed: MischiefManager is missing.");
            return;
        }

        mischiefManager.StartMischiefTargetCooldown(targetId, duration);
    }

    public void DisableMischiefTarget(string targetId)
    {
        if (mischiefManager == null)
        {
            Debug.LogWarning("CoreFacade.DisableMischiefTarget failed: MischiefManager is missing.");
            return;
        }

        mischiefManager.DisableMischiefTarget(targetId);
    }

    public void LockMischiefTarget(string targetId)
    {
        if (mischiefManager == null)
        {
            Debug.LogWarning("CoreFacade.LockMischiefTarget failed: MischiefManager is missing.");
            return;
        }

        mischiefManager.LockMischiefTarget(targetId);
    }

    public void UnlockMischiefTarget(string targetId)
    {
        if (mischiefManager == null)
        {
            Debug.LogWarning("CoreFacade.UnlockMischiefTarget failed: MischiefManager is missing.");
            return;
        }

        mischiefManager.UnlockMischiefTarget(targetId);
    }

    public float GetTargetCooldownRemaining(string targetId)
    {
        return mischiefManager != null ? mischiefManager.GetTargetCooldownRemaining(targetId) : 0f;
    }

    public void RegisterRageReceiver(IRageReceiver receiver)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.RegisterRageReceiver failed: RageManager is missing.");
            return;
        }

        rageManager.RegisterNpc(receiver);
    }

    public void RegisterRageReceiver(string npcId, object receiverObject)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.RegisterRageReceiver failed: RageManager is missing.");
            return;
        }

        rageManager.RegisterNpc(npcId, receiverObject);
    }

    public void UnregisterRageReceiver(IRageReceiver receiver)
    {
        if (receiver == null)
        {
            return;
        }

        UnregisterRageReceiver(receiver.NpcId);
    }

    public void UnregisterRageReceiver(string npcId)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.UnregisterRageReceiver failed: RageManager is missing.");
            return;
        }

        rageManager.UnregisterNpc(npcId);
    }

    public float GetRage(string npcId)
    {
        return rageManager != null ? rageManager.GetRage(npcId) : 0f;
    }

    public NpcRageState GetRageState(string npcId)
    {
        return rageManager != null ? rageManager.GetRageState(npcId) : NpcRageState.Calm;
    }

    public float GetAverageRage()
    {
        return rageManager != null ? rageManager.GetAverageRage() : 0f;
    }

    public List<string> GetRegisteredNpcIds()
    {
        return rageManager != null ? rageManager.GetRegisteredNpcIds() : new List<string>();
    }

    public bool TryGetNpcWorldPosition(string npcId, out Vector3 position)
    {
        position = Vector3.zero;
        return rageManager != null && rageManager.TryGetNpcPosition(npcId, out position);
    }

    public RageResult ReduceNpcRage(string npcId, float amount)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.ReduceNpcRage failed: RageManager is missing.");
            return new RageResult(npcId, 0f, 0f, NpcRageState.Calm, NpcRageState.Calm, false);
        }

        return rageManager.ReduceRage(npcId, amount);
    }

    public void SetNpcRage(string npcId, float value)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.SetNpcRage failed: RageManager is missing.");
            return;
        }

        rageManager.SetRage(npcId, value);
    }

    public RageResult ReportNpcLostPlayer(string npcId)
    {
        return ReduceNpcRage(npcId, 10f);
    }

    public List<RageResult> TryCuteAction(Vector3 origin)
    {
        return TryCuteAction(origin, cuteActionRadius, cuteActionRageReduction);
    }

    public List<RageResult> TryCuteAction(Vector3 origin, float radius, float rageReduction)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.TryCuteAction failed: RageManager is missing.");
            return new List<RageResult>();
        }

        return rageManager.ReduceRageAround(origin, radius, rageReduction, excludeSecurity: true);
    }

    public bool CanUseHideSpot(string hideSpotId)
    {
        return hidingManager != null && hidingManager.CanUseHideSpot(hideSpotId);
    }

    public bool ReportPlayerHidden(string hideSpotId)
    {
        if (hidingManager == null)
        {
            Debug.LogWarning("CoreFacade.ReportPlayerHidden failed: HidingManager is missing.");
            return false;
        }

        return hidingManager.ReportPlayerHidden(hideSpotId);
    }

    public void ReportPlayerExitHiding()
    {
        if (hidingManager == null)
        {
            Debug.LogWarning("CoreFacade.ReportPlayerExitHiding failed: HidingManager is missing.");
            return;
        }

        hidingManager.ReportPlayerExitHiding();
    }

    public bool HasUsedHideSpot(string hideSpotId)
    {
        return hidingManager != null && hidingManager.HasUsedHideSpot(hideSpotId);
    }

    public void ReportPlayerCaught()
    {
        if (failManager == null)
        {
            Debug.LogWarning("CoreFacade.ReportPlayerCaught failed: FailManager is missing.");
            return;
        }

        failManager.HandlePlayerCaught();
    }

    public int AddScore(int amount)
    {
        if (scoreManager == null)
        {
            Debug.LogWarning("CoreFacade.AddScore failed: ScoreManager is missing.");
            return 0;
        }

        return scoreManager.AddScore(amount);
    }

    public void SetSecurityMultiplierOverride(bool enabled)
    {
        SetSecurityMultiplierOverride(enabled, defaultSecurityMultiplier);
    }

    public void SetSecurityMultiplierOverride(bool enabled, float multiplier)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.SetSecurityMultiplierOverride failed: RageManager is missing.");
            return;
        }

        rageManager.SetSecurityMultiplierOverride(enabled, multiplier);
    }

    public void RegisterMischiefWorldEventTarget(IMischiefWorldEventTarget target)
    {
        if (target == null || string.IsNullOrWhiteSpace(target.WorldEventTargetId))
        {
            return;
        }

        worldEventTargets[target.WorldEventTargetId] = target;
    }

    public void UnregisterMischiefWorldEventTarget(IMischiefWorldEventTarget target)
    {
        if (target == null || string.IsNullOrWhiteSpace(target.WorldEventTargetId))
        {
            return;
        }

        if (worldEventTargets.TryGetValue(target.WorldEventTargetId, out IMischiefWorldEventTarget current) && ReferenceEquals(current, target))
        {
            worldEventTargets.Remove(target.WorldEventTargetId);
        }
    }

    public bool TryGetMischiefWorldEventTarget(string targetId, out IMischiefWorldEventTarget target)
    {
        target = null;
        return !string.IsNullOrWhiteSpace(targetId) && worldEventTargets.TryGetValue(targetId, out target) && target != null;
    }

    public MischiefWorldEventResult ReportMischiefEventFromMischief(MischiefContext context)
    {
        MischiefWorldEventType eventType = MischiefWorldEventContext.InferEventType(context.TargetId, context.MischiefType);
        if (eventType == MischiefWorldEventType.None)
        {
            return MischiefWorldEventResult.Ignored(context.TargetId, eventType, context.Position, "No world event route for this target.");
        }

        MischiefWorldEventContext eventContext = new MischiefWorldEventContext(
            context.ActorId,
            context.TargetId,
            eventType,
            context.Position,
            MischiefWorldEventContext.GetDefaultResolveMode(eventType),
            string.Empty,
            NpcType.Special,
            MischiefWorldEventContext.ShouldDisableTargetAfterResponse(eventType));

        return ReportMischiefWorldEvent(eventContext);
    }

    public MischiefWorldEventResult ReportMischiefWorldEvent(string targetId, MischiefWorldEventType eventType, Vector3 position)
    {
        MischiefWorldEventContext context = new MischiefWorldEventContext(
            "World",
            targetId,
            eventType,
            position,
            MischiefWorldEventContext.GetDefaultResolveMode(eventType),
            string.Empty,
            NpcType.Special,
            MischiefWorldEventContext.ShouldDisableTargetAfterResponse(eventType));

        return ReportMischiefWorldEvent(context);
    }

    public MischiefWorldEventResult ReportMischiefWorldEvent(MischiefWorldEventContext context)
    {
        if (rageManager == null)
        {
            Debug.LogWarning("CoreFacade.ReportMischiefWorldEvent failed: RageManager is missing.");
            return MischiefWorldEventResult.Ignored(context.TargetId, context.EventType, context.Position, "RageManager is missing.");
        }

        MischiefWorldEventType eventType = context.EventType;
        if (eventType == MischiefWorldEventType.Auto)
        {
            eventType = MischiefWorldEventContext.InferEventType(context.TargetId, MischiefType.Custom);
            context = context.WithEventType(eventType);
        }

        if (eventType == MischiefWorldEventType.None)
        {
            return MischiefWorldEventResult.Ignored(context.TargetId, eventType, context.Position, "No world event route for this target.");
        }

        MischiefWorldEventResolveMode resolveMode = context.ResolveMode;
        if (resolveMode == MischiefWorldEventResolveMode.None)
        {
            resolveMode = MischiefWorldEventContext.GetDefaultResolveMode(eventType);
            context = context.WithResolveMode(resolveMode);
        }

        if (TryGetMischiefWorldEventTarget(context.TargetId, out IMischiefWorldEventTarget eventTarget) && !eventTarget.CanStartWorldEvent())
        {
            string reason = eventTarget.GetUnavailableReason();
            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = context.TargetId + " cannot start world event now.";
            }

            Debug.LogWarning("CoreFacade.ReportMischiefWorldEvent: " + reason);
            return MischiefWorldEventResult.Ignored(context.TargetId, eventType, context.Position, reason);
        }

        // Start and lock the world event before NPC routing.
        // The object state must change even if no NPC is currently available or the NPC API is not implemented yet.
        MarkMischiefWorldEventStarted(context);

        string reactorNpcId;
        IRageReceiver reactorReceiver;
        bool hasUniqueReactor = TryResolveWorldEventReceiver(context, resolveMode, out reactorNpcId, out reactorReceiver)
            && reactorReceiver != null
            && !string.IsNullOrWhiteSpace(reactorNpcId);

        if (!hasUniqueReactor)
        {
            reactorNpcId = string.Empty;
            Debug.LogWarning("CoreFacade.ReportMischiefWorldEvent: No unique reactor found for " + eventType + ". Broadcasting with shouldReact=false for all NPCs.");
        }

        MischiefWorldEventResult result = DispatchWorldEventGlobally(context, hasUniqueReactor ? reactorNpcId : string.Empty);
        if (!result.Dispatched)
        {
            return MischiefWorldEventResult.Routed(context.TargetId, eventType, hasUniqueReactor ? reactorNpcId : "PendingNPC", context.Position, 0);
        }

        return result;
    }

    public void CompleteMischiefWorldEvent(string targetId)
    {
        bool disableTarget = true;
        if (!string.IsNullOrWhiteSpace(targetId) && pendingWorldEventDisableByTargetId.TryGetValue(targetId, out bool pendingDisable))
        {
            disableTarget = pendingDisable;
        }

        CompleteMischiefWorldEvent(targetId, disableTarget, 0f);
    }

    public void CompleteMischiefWorldEvent(string targetId, bool disableTarget, float cooldownDuration)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            Debug.LogWarning("CoreFacade.CompleteMischiefWorldEvent failed: targetId is empty.");
            return;
        }

        pendingWorldEventDisableByTargetId.Remove(targetId);

        if (worldEventTargets.TryGetValue(targetId, out IMischiefWorldEventTarget eventTarget) && eventTarget != null)
        {
            eventTarget.OnWorldEventCompleted(disableTarget, cooldownDuration);
        }

        if (disableTarget)
        {
            DisableMischiefTarget(targetId);
            return;
        }

        if (cooldownDuration > 0f)
        {
            StartMischiefTargetCooldown(targetId, cooldownDuration);
            return;
        }

        SetMischiefTargetState(targetId, MischiefTargetState.Available);
    }

    public void ReportMischiefEventCompleted(string targetId)
    {
        CompleteMischiefWorldEvent(targetId, true, 0f);
    }

    public void ReportMischiefEventCompleted(string targetId, bool disableTarget, float cooldownDuration)
    {
        CompleteMischiefWorldEvent(targetId, disableTarget, cooldownDuration);
    }

    private void MarkMischiefWorldEventStarted(MischiefWorldEventContext context)
    {
        if (string.IsNullOrWhiteSpace(context.TargetId))
        {
            return;
        }

        SetMischiefTargetState(context.TargetId, MischiefTargetState.Locked);
        pendingWorldEventDisableByTargetId[context.TargetId] = context.DisableTargetAfterResponse;

        if (worldEventTargets.TryGetValue(context.TargetId, out IMischiefWorldEventTarget eventTarget) && eventTarget != null)
        {
            eventTarget.OnWorldEventStarted(context);
        }
    }

    private bool TryResolveWorldEventReceiver(MischiefWorldEventContext context, MischiefWorldEventResolveMode resolveMode, out string npcId, out IRageReceiver receiver)
    {
        npcId = string.Empty;
        receiver = null;

        if (rageManager == null)
        {
            return false;
        }

        switch (resolveMode)
        {
            case MischiefWorldEventResolveMode.SpecificNpc:
                return !string.IsNullOrWhiteSpace(context.PreferredNpcId)
                    && rageManager.TryGetReceiver(context.PreferredNpcId, out receiver)
                    && SetResolvedNpcId(context.PreferredNpcId, out npcId);

            case MischiefWorldEventResolveMode.NearestCleaner:
                return rageManager.TryFindNearestNpcOfType(NpcType.Cleaner, context.Position, out npcId, out receiver);

            case MischiefWorldEventResolveMode.NearestNpc:
                return rageManager.TryFindNearestNpc(context.Position, out npcId, out receiver, excludeSecurity: true);

            default:
                return false;
        }
    }

    private bool SetResolvedNpcId(string value, out string npcId)
    {
        npcId = value;
        return true;
    }

    private MischiefWorldEventResult DispatchWorldEventGlobally(MischiefWorldEventContext context, string reactorNpcId)
    {
        List<string> ids = rageManager.GetRegisteredNpcIds();
        int dispatchedCount = 0;
        int reactorTrueCount = 0;
        string firstNpcId = string.Empty;
        string assignedNpcId = string.IsNullOrWhiteSpace(reactorNpcId) ? "PendingNPC" : reactorNpcId;

        for (int i = 0; i < ids.Count; i++)
        {
            string currentNpcId = ids[i];
            if (!rageManager.TryGetReceiver(currentNpcId, out IRageReceiver receiver) || receiver == null)
            {
                continue;
            }

            bool shouldReact = !string.IsNullOrWhiteSpace(reactorNpcId) && currentNpcId == reactorNpcId;
            if (shouldReact)
            {
                reactorTrueCount++;
            }

            MischiefWorldEventContext npcContext = context.WithReactionAssignment(shouldReact, assignedNpcId);
            if (DispatchWorldEventToReceiver(receiver, npcContext))
            {
                if (string.IsNullOrEmpty(firstNpcId))
                {
                    firstNpcId = currentNpcId;
                }

                dispatchedCount++;
            }
        }

        if (reactorTrueCount > 1)
        {
            Debug.LogError("CoreFacade.DispatchWorldEventGlobally assigned shouldReact=true to more than one NPC. This should never happen.");
        }

        if (dispatchedCount == 0)
        {
            return MischiefWorldEventResult.Ignored(context.TargetId, context.EventType, context.Position, "No NPC accepted the global world event.");
        }

        return MischiefWorldEventResult.Routed(context.TargetId, context.EventType, assignedNpcId, context.Position, dispatchedCount);
    }

    private bool DispatchWorldEventToReceiver(IRageReceiver receiver, MischiefWorldEventContext context)
    {
        if (receiver == null)
        {
            return false;
        }

        if (receiver is IMischiefWorldEventReceiver typedReceiver)
        {
            typedReceiver.OnMischiefWorldEvent(context);
            return true;
        }

        object receiverObject = receiver;
        if (TryInvoke(receiverObject, "OnMischiefWorldEvent", context)) return true;
        if (TryInvoke(receiverObject, "OnMischiefEvent", context)) return true;
        if (TryInvoke(receiverObject, "HandleMischiefWorldEvent", context)) return true;

        // Compatibility fallback methods do not receive shouldReact.
        // Only call them for the unique reactor to avoid making every NPC perform the main reaction.
        if (!context.ShouldReact)
        {
            return false;
        }

        if (context.EventType == MischiefWorldEventType.LightToggle)
        {
            if (TryInvoke(receiverObject, "OnLightEvent", context.TargetId, context.Position)) return true;
            if (TryInvoke(receiverObject, "OnLightEvent", context.TargetId, context.Position, context.EventType)) return true;
        }
        else
        {
            if (TryInvoke(receiverObject, "OnMessEvent", context.TargetId, context.Position, context.EventType)) return true;
            if (TryInvoke(receiverObject, "OnMessEvent", context.TargetId, context.Position)) return true;
        }

        return false;
    }

    private bool TryInvoke(object target, string methodName, params object[] arguments)
    {
        if (target == null)
        {
            return false;
        }

        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo[] methods = target.GetType().GetMethods(flags);
        for (int i = 0; i < methods.Length; i++)
        {
            MethodInfo method = methods[i];
            if (method.Name != methodName)
            {
                continue;
            }

            ParameterInfo[] parameters = method.GetParameters();
            if (parameters.Length != arguments.Length)
            {
                continue;
            }

            bool matches = true;
            for (int j = 0; j < parameters.Length; j++)
            {
                if (arguments[j] != null && !parameters[j].ParameterType.IsInstanceOfType(arguments[j]))
                {
                    matches = false;
                    break;
                }
            }

            if (!matches)
            {
                continue;
            }

            method.Invoke(target, arguments);
            return true;
        }

        return false;
    }

    public bool ValidateCoreReferences(out string report)
    {
        List<string> missing = new List<string>();

        if (gameManager == null) missing.Add("GameManager");
        if (stageManager == null) missing.Add("StageManager");
        if (mischiefManager == null) missing.Add("MischiefManager");
        if (scoreManager == null) missing.Add("ScoreManager");
        if (rageManager == null) missing.Add("RageManager");
        if (objectiveManager == null) missing.Add("ObjectiveManager");
        if (failManager == null) missing.Add("FailManager");

        if (missing.Count == 0)
        {
            report = hidingManager == null
                ? "CoreFacade references are valid. HidingManager is missing, so hiding support is disabled."
                : "CoreFacade references are valid.";
            return true;
        }

        report = "CoreFacade is missing required references:";
        for (int i = 0; i < missing.Count; i++)
        {
            report += "\n- " + missing[i];
        }

        return false;
    }
}
