using System.Collections.Generic;
using UnityEngine;

public class NpcController : MonoBehaviour, IRageReceiver
{
    private CoreFacade coreFacade;

    [Header("Params")]
    [SerializeField] NpcData npcData;

    [Header("Debug - State Machine")]
    [SerializeField] NpcRageState debugRageState;
    [SerializeField] NpcState StateToSwitch;

    // Components
    private NpcNavigate npcNavigate;
    private NpcCatch npcCatch;
    private NpcView npcView;
    private NpcStateMachine npcStateMachine;

    // behaviors
    private NpcIdleBehavior npcIdleBehavior;
    private NpcChaseBehavior npcChaseBehavior;
    private NpcSearchBehavior npcSearchBehavior;
    private NpcDiveBehavior npcDiveBehavior;
    private NpcOverrideBehavior npcOverrideBehavior;
    private NpcCooldownBehavior npcCooldownBehavior;

    // Implemented public APIs:
    public NpcData NpcData => npcData;
    public string NpcId => npcData != null ? npcData.npcId : gameObject.name;
    public Vector3 Position => transform.position;
    public NpcType NpcType => npcData != null ? npcData.npcType : NpcType.Worker;
    public bool CanReceiveRage => npcData == null || npcData.canReceiveRage;

    // NpcState
    public NpcState CurrentNpcState
    {
        get
        {
            return _npcState;
        }
        set
        {
            if (value == _npcState) return;
            ResolveNpcStateChange(_npcState, value);
            _npcState = value;
        }
    }
        private NpcState _npcState;

    // NpcState flags
    [HideInInspector] public NpcRageState CurrentRageState;
    [HideInInspector] public bool PlayerInView;
    [HideInInspector] public bool PlayerInReach;
    public bool IsTired
    {
        get
        {
            LazyInitialize();
            return _isTired;
        }
        set
        {
            if (value == _isTired) return;
            _isTired = value;
            OnNpcStateFlagsChange();
        }
    }
        private bool _isTired;

    public bool IsOverride
    {
        get
        {
            LazyInitialize();
            return _isOverride;
        }
        set
        {
            if (value == _isOverride) return;
            _isOverride = value;
            OnNpcStateFlagsChange();
        }
    }
        private bool _isOverride;

    // behavior delegates
    private delegate void IdleBehavior();
    private delegate void ChaseBehavior();
    private delegate void SearchBehavior();
    private delegate void DiveBehavior();
    private delegate void OverrideBehavior();
    private delegate void CooldownBehavior();
    private IdleBehavior idleAction;
    private ChaseBehavior chaseAction;
    private SearchBehavior searchAction;
    private DiveBehavior diveAction;
    private CooldownBehavior cooldownAction;
    private OverrideBehavior overrideAction;
    private delegate void ExitIdleBehavior();
    private delegate void ExitChaseBehavior();
    private delegate void ExitSearchBehavior();
    private delegate void ExitDiveBehavior();
    private delegate void ExitOverrideBehavior();
    private delegate void ExitCooldownBehavior();
    private ExitIdleBehavior exitIdleAction;
    private ExitChaseBehavior exitChaseAction;
    private ExitSearchBehavior exitSearchAction;
    private ExitDiveBehavior exitDiveAction;
    private ExitCooldownBehavior exitCooldownAction;
    private ExitOverrideBehavior exitOverrideAction;


    private void Awake()
    {
        LazyInitialize();
        AssignBehaviors();
        AssignExitBehaviors();
    }

    private void OnEnable()
    {
        coreFacade = FindFirstObjectByType<CoreFacade>();
        coreFacade.RegisterRageReceiver(this);

        SubscribeFlagChange();
    }

    private void OnDisable()
    {
        coreFacade.UnregisterRageReceiver(this);

        UnsubscribeFlagChange();
    }

    private void Start()
    {
        idleAction();
    }

    private void Update()
    {
        RefreshNpcStateFlags();
    }

    private void OnNpcStateFlagsChange()
    {
        LazyInitialize();

        RefreshNpcStateFlags();

        NpcStateSnapshot snapshot = new(
            CurrentNpcState,
            CurrentRageState,
            PlayerInView,
            IsTired,
            IsOverride,
            PlayerInReach
        );

        npcStateMachine.ResolveFlagChange(snapshot);
    }

    public void SwitchNpcState(NpcState npcState)
    {
        CurrentNpcState = npcState;
    }

    private void ResolveNpcStateChange(NpcState prevState, NpcState currentState)
    {
        Debug.Log($"[NPC] {NpcId}: NpcState changes from {prevState} to {currentState}");
        // exit behavior of previous state
        switch (prevState)
        {
            case NpcState.Override:
                exitOverrideAction();
                break;
            case NpcState.Idle:
                exitIdleAction();
                break;
            case NpcState.Chase:
                exitChaseAction();
                break;
            case NpcState.Dive:
                exitDiveAction();
                break;
            case NpcState.Search:
                exitSearchAction();
                break;
            case NpcState.Cooldown:
                exitCooldownAction();
                break;
        }

        // next state's behavior
        switch (currentState)
        {
            case NpcState.Override:
                Debug.Log($"[NPC] {NpcId}: perform override behavior");
                overrideAction(); 
                break;
            case NpcState.Idle:
                Debug.Log($"[NPC] {NpcId}: perform idle behavior");
                idleAction();
                break;
            case NpcState.Chase:
                Debug.Log($"[NPC] {NpcId}: perform chase behavior");
                chaseAction();
                break;
            case NpcState.Dive:
                Debug.Log($"[NPC] {NpcId}: perform dive behavior");
                diveAction();
                break;
            case NpcState.Search:
                Debug.Log($"[NPC] {NpcId}: perform search behavior");
                searchAction();
                break;
            case NpcState.Cooldown:
                Debug.Log($"[NPC] {NpcId}: perform cooldown behavior");
                npcCooldownBehavior.EnterFrom(prevState);
                break;
        }
    }

    public void SetRageState(NpcRageState state)
    {
        CurrentRageState = state;
        Debug.Log($"[NPC] {NpcId}: RageState changes to {state}");
    }

    public void StartChase()
    {
        OnNpcStateFlagsChange();
        // this is effectively "ResolveRageStateChange()"
    }

    public void StopChase()
    {
        // this is effectively "ResolveRageStateChangeOver()"
        OnNpcStateFlagsChange();
    }

    public void LoseTarget()
    {
        StopChase();

        // TODO: Start wandering/searching behavior.
    }

    public void OnPlayerCaught(PlayerController player)
    {
        if (player != null)
            player.PlayCaught();
    }

    public void AssignBehaviors()
    {
        switch (NpcType)
        {
            case NpcType.Supervisor:
                idleAction = npcIdleBehavior.Supervisor;
                chaseAction = npcChaseBehavior.Supervisor;
                searchAction = npcSearchBehavior.Supervisor;
                diveAction = npcDiveBehavior.Supervisor;
                overrideAction = npcOverrideBehavior.Supervisor;
                cooldownAction = npcCooldownBehavior.Supervisor;
                break;
            case NpcType.Worker:
                idleAction = npcIdleBehavior.Worker;
                chaseAction = npcChaseBehavior.Worker;
                searchAction = npcSearchBehavior.Worker;
                diveAction = npcDiveBehavior.Worker;
                overrideAction = npcOverrideBehavior.Worker;
                cooldownAction = npcCooldownBehavior.Worker;
                break;
            case NpcType.Cleaner:
                idleAction = npcIdleBehavior.Cleaner;
                chaseAction = npcChaseBehavior.Cleaner;
                searchAction = npcSearchBehavior.Cleaner;
                diveAction = npcDiveBehavior.Cleaner;
                overrideAction = npcOverrideBehavior.Cleaner;
                cooldownAction = npcCooldownBehavior.Cleaner;
                break;
            case NpcType.Security:
                idleAction = npcIdleBehavior.Security;
                chaseAction = npcChaseBehavior.Security;
                searchAction = npcSearchBehavior.Security;
                diveAction = npcDiveBehavior.Security;
                overrideAction = npcOverrideBehavior.Security;
                cooldownAction = npcCooldownBehavior.Security;
                break;
            default:
                Debug.LogWarning($"No behavior defined for {NpcType}, using Supervisor defaults.");
                goto case NpcType.Supervisor;
        }
    }

    public void OnSnapshotRequest()
    {
        OnNpcStateFlagsChange();
    }

    public void AssignExitBehaviors()
    {
        exitIdleAction = npcIdleBehavior.ExitState;
        exitChaseAction = npcChaseBehavior.ExitState;
        exitSearchAction = npcSearchBehavior.ExitState;
        exitDiveAction = npcDiveBehavior.ExitState;
        exitOverrideAction = npcOverrideBehavior.ExitState;
        exitCooldownAction = npcCooldownBehavior.ExitState;
    }

    private void LazyInitialize()
    {
        if (npcCatch == null) npcCatch = GetComponent<NpcCatch>();
        if (npcNavigate == null) npcNavigate = GetComponent<NpcNavigate>();
        if (npcView == null) npcView = GetComponent<NpcView>();
        if (npcOverrideBehavior == null) npcOverrideBehavior = GetComponent<NpcOverrideBehavior>();
        if (npcIdleBehavior == null) npcIdleBehavior = GetComponent<NpcIdleBehavior>();
        if (npcChaseBehavior == null) npcChaseBehavior = GetComponent<NpcChaseBehavior>();
        if (npcSearchBehavior == null) npcSearchBehavior = GetComponent<NpcSearchBehavior>();
        if (npcDiveBehavior == null) npcDiveBehavior = GetComponent<NpcDiveBehavior>();
        if (npcCooldownBehavior == null) npcCooldownBehavior = GetComponent<NpcCooldownBehavior>();
        if (npcStateMachine == null) npcStateMachine = GetComponent<NpcStateMachine>();
    }

    private void SubscribeFlagChange()
    {
        npcView.PlayerInViewFlagChange += OnNpcStateFlagsChange;
        npcView.PlayerInReachFlagChange += OnNpcStateFlagsChange;
    }

    private void UnsubscribeFlagChange()
    {
        npcView.PlayerInViewFlagChange -= OnNpcStateFlagsChange;
        npcView.PlayerInReachFlagChange -= OnNpcStateFlagsChange;
    }

    private void RefreshNpcStateFlags()
    {
        PlayerInView = npcView.PlayerInView;
        PlayerInReach = npcView.PlayerInReach;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(255, 0, 0);
        Gizmos.DrawRay(transform.position, transform.forward * 3.5f);
        Gizmos.color = new Color(255, 0, 0);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, 70f, 0f) * transform.forward * 3.5f);
        Gizmos.color = new Color(255, 0, 0);
        Gizmos.DrawRay(transform.position, Quaternion.Euler(0f, -70f, 0f) * transform.forward * 3.5f);
    }


    [ContextMenu("Debug Test Finish CurrentState")]
    private void FinishCurrentState()
    {
        npcStateMachine.NotifyStateFinished();
    }

    [ContextMenu("Debug Test RequestTransition")]
    private void RequestTransition()
    {
        npcStateMachine.RequestTransition(StateToSwitch);
    }
}
public enum NpcState
{
    Idle,
    Chase,
    Search,
    Dive,
    Cooldown,
    Override
}

public readonly struct NpcStateSnapshot
{
    public readonly NpcState currentState;
    public readonly NpcRageState currentRageState;
    public readonly bool currentPlayerInView;
    public readonly bool currentIsTired;
    public readonly bool currentIsOverride;
    public readonly bool currentPlayerInReach;

    public NpcStateSnapshot(NpcState currentState,
        NpcRageState currentRageState,
        bool currentPlayerInView,
        bool currentIsTired, 
        bool currentIsOverride,
        bool currentPlayerInReach)
    {
        this.currentState = currentState;
        this.currentPlayerInView = currentPlayerInView;
        this.currentRageState = currentRageState;
        this.currentIsTired = currentIsTired;
        this.currentIsOverride = currentIsOverride;
        this.currentPlayerInReach = currentPlayerInReach;
    }
}