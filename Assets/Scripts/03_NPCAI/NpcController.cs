using System.Collections.Generic;
using UnityEngine;

public class NpcController : MonoBehaviour, IRageReceiver
{
    [Header("Core References")]
    [SerializeField] CoreFacade coreFacade;

    [Header("Params")]
    [SerializeField] NpcData npcData;

    // Components:
    private NpcNavigate npcNavigate;
    private NpcCatch npcCatch;
    private NpcPerception npcPerception;

    // Implemented public APIs:
    public NpcData NpcData => npcData;
    public string NpcId => npcData != null ? npcData.npcId : gameObject.name;
    public Vector3 Position => transform.position;
    public NpcType NpcType => npcData != null ? npcData.npcType : NpcType.Worker;
    public bool CanReceiveRage => npcData == null || npcData.canReceiveRage;
    public bool IsChasing { get; private set; }
    public NpcRageState CurrentRageState { get; private set; } = NpcRageState.Calm;

    [Header("Debug")]
    public GameObject target;

    private void Awake()
    {
        Initialize(npcData);
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

    public void Initialize(NpcData data)
    {
        npcData = data;

        npcCatch = GetComponent<NpcCatch>();
        npcNavigate = GetComponent<NpcNavigate>();
        npcPerception = GetComponent<NpcPerception>();
    }

    public void SetRageState(NpcRageState state)
    {
        CurrentRageState = state;

        // TODO: Update NPC animation, expression, and behavior state.
    }

    public void StartChase()
    {
        IsChasing = true;

        npcNavigate.StartChase(target);
    }

    public void StopChase()
    {
        IsChasing = false;

        npcNavigate.StopChase();
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
}
