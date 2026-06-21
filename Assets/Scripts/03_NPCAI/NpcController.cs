using UnityEngine;

public class NpcController : MonoBehaviour
{
    [SerializeField] private NpcData npcData;
    [SerializeField] private RageManager rageManager;
    [SerializeField] private NpcChase npcChase;

    public NpcData NpcData => npcData;
    public string NpcId => npcData != null ? npcData.npcId : gameObject.name;
    public NpcType NpcType => npcData != null ? npcData.npcType : NpcType.Worker;
    public bool CanReceiveRage => npcData == null || npcData.canReceiveRage;
    public bool IsChasing { get; private set; }
    public NpcRageState CurrentRageState { get; private set; } = NpcRageState.Calm;

    private void Start()
    {
        if (rageManager != null)
            rageManager.RegisterNpc(NpcId, this);
    }

    private void OnDestroy()
    {
        if (rageManager != null)
            rageManager.UnregisterNpc(NpcId);
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
