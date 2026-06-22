using UnityEngine;

public interface IRageReceiver
{
    string NpcId { get; }
    NpcData NpcData { get; }
    bool CanReceiveRage { get; }
    Vector3 Position { get; }

    void SetRageState(NpcRageState state);
    void StartChase();
    void StopChase();
    void LoseTarget();
}
