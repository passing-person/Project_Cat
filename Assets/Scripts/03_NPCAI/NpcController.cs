using System.Collections.Generic;
using UnityEngine;

public class NpcController : MonoBehaviour, IRageReceiver
{
    [Header("Core References")]
    [SerializeField] CoreFacade coreFacade;

    [Header("Params")]
    [SerializeField] NpcData npcData;

    [Header("Debug")]
    [SerializeField] NpcRageState debugRageState;

    // runtime
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
    public bool PlayerInView
    {
        get
        {
            LazyInitialize();
            return npcView.PlayerInView;
        }
    }

    // Components
    private NpcNavigate npcNavigate;
    private NpcCatch npcCatch;
    private NpcView npcView;

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
    
    // runtime fields
    public NpcRageState CurrentRageState
    {
        get
        {
            return _rageState;
        }
        set
        {
            if (value == _rageState) return;
            // No need to call ResolveRageStateChange() because RageManager calls it for you.
            _rageState = value;
        }
    }
        private NpcRageState _rageState;

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
    private OverrideBehavior overrideAction;
    private CooldownBehavior cooldownAction;


    [Header("Debug")]
    public GameObject target;

    private void Awake()
    {
        LazyInitialize();
        AssignBehaviors();
    }

    private void OnEnable()
    {
        coreFacade = FindFirstObjectByType<CoreFacade>();

        coreFacade.RegisterRageReceiver(this);
    }

    private void OnDisable()
    {
        coreFacade.UnregisterRageReceiver(this);
    }

    private void Update()
    {
    }

    private void UpdateNpcState()
    {

    }

    private void ResolveNpcStateChange(NpcState prevState, NpcState currentState)
    {
        Debug.Log($"[NPC] {NpcId}: NpcState changes from {prevState} to {currentState}");
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
        }
    }

    public void SetRageState(NpcRageState state)
    {
        CurrentRageState = state;
        Debug.Log($"[NPC] {NpcId}: RageState changes to {state}");
        // TODO: Update NPC animation, expression, and behavior state.
    }

    public void StartChase()
    {
        // this is effectively "ResolveRageStateChange()"
    }

    public void StopChase()
    {
        // this is effectively "ResolveRageStateChangeOver()"
        npcNavigate.StopNav();
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
                break;
            case NpcType.Cleaner:
                idleAction = npcIdleBehavior.Cleaner;
                chaseAction = npcChaseBehavior.Cleaner;
                searchAction = npcSearchBehavior.Cleaner;
                diveAction = npcDiveBehavior.Cleaner;
                overrideAction = npcOverrideBehavior.Cleaner;
                break;
            case NpcType.Security:
                idleAction = npcIdleBehavior.Security;
                chaseAction = npcChaseBehavior.Security;
                searchAction = npcSearchBehavior.Security;
                diveAction = npcDiveBehavior.Security;
                overrideAction = npcOverrideBehavior.Security;
                break;
            default:
                Debug.LogWarning($"No behavior defined for {NpcType}, using Supervisor defaults.");
                goto case NpcType.Supervisor;
        }
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

    public enum NpcState
    {
        Idle,
        Chase,
        Search,
        Dive,
        Cooldown,
        Override
    }

    [ContextMenu("Debug Test RageState")]
    private void DebugTestRageState()
    {
        SetRageState(debugRageState);
    }
    [ContextMenu("Debug Test Delegates")]
    private void DebugTestDelegates()
    {
        idleAction();
        chaseAction();
        diveAction();
        searchAction();
        overrideAction();
        cooldownAction();
    }
}