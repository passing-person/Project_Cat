using System.Collections.Generic;
using UnityEngine;

public class NpcController : MonoBehaviour
{
    [SerializeField] NpcData npcData;
    [SerializeField] CoreFacade coreFacade;
    [SerializeField] NpcChase npcChase;

    public NpcData NpcData => npcData;
    public string NpcId => npcData != null ? npcData.npcId : gameObject.name;
    public NpcType NpcType => npcData != null ? npcData.npcType : NpcType.Worker;
    public bool CanReceiveRage => npcData == null || npcData.canReceiveRage;
    public bool IsChasing { get; private set; }
    public NpcRageState CurrentRageState { get; private set; } = NpcRageState.Calm;

    private void Awake()
    {
        Initialize(npcData);
    }

    private void OnEnable()
    {
        coreFacade = FindFirstObjectByType<CoreFacade>();

        coreFacade.RegisterRageReceiver(this as IRageReceiver);
    }

    private void OnDisable()
    {
        coreFacade.UnregisterRageReceiver(this as IRageReceiver);
    }

    public void Initialize(NpcData data)
    {
        npcData = data;
    }

    public void SetRageState(NpcRageState state)
    {
        CurrentRageState = state;

        // TODO: Update NPC animation, expression, and behavior state.
    }

    public void StartChase()
    {
        IsChasing = true;

        if (npcChase != null)
            npcChase.StartChase();
    }

    public void StopChase()
    {
        IsChasing = false;

        if (npcChase != null)
            npcChase.StopChase();
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
