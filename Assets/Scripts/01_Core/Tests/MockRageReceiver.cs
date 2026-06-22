using UnityEngine;

public class MockRageReceiver : MonoBehaviour, IRageReceiver
{
    [Header("Mock NPC")]
    public string npcId = "Supervisor";
    public NpcData npcData;
    public bool canReceiveRage = true;

    [Header("Debug")]
    public NpcRageState currentState = NpcRageState.Calm;
    public bool startChaseCalled;
    public bool stopChaseCalled;
    public bool loseTargetCalled;

    public string NpcId => npcId;
    public NpcData NpcData => npcData;
    public bool CanReceiveRage => canReceiveRage;
    public Vector3 Position => transform.position;

    public void SetRageState(NpcRageState state)
    {
        currentState = state;
        Debug.Log($"[MockRageReceiver] {npcId} state = {state}");
    }

    public void StartChase()
    {
        startChaseCalled = true;
        Debug.Log($"[MockRageReceiver] {npcId} StartChase called.");
    }

    public void StopChase()
    {
        stopChaseCalled = true;
        Debug.Log($"[MockRageReceiver] {npcId} StopChase called.");
    }

    public void LoseTarget()
    {
        loseTargetCalled = true;
        Debug.Log($"[MockRageReceiver] {npcId} LoseTarget called.");
    }

    public void ResetMockFlags()
    {
        currentState = NpcRageState.Calm;
        startChaseCalled = false;
        stopChaseCalled = false;
        loseTargetCalled = false;
    }
}
