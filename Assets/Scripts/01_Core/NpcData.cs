using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/NPC Data")]
public class NpcData : ScriptableObject
{
    [Header("Identity")]
    public string npcId = "Supervisor";
    public NpcType npcType = NpcType.Supervisor;

    [Header("Movement")]
    public float moveSpeed = 2f;
    public float chaseSpeed = 4f;

    [Header("Rage Thresholds")]
    public float annoyedThreshold = 40f;
    public float angryThreshold = 70f;
    public float enragedThreshold = 100f;

    [Header("Rage")]
    public bool canReceiveRage = true;
}
