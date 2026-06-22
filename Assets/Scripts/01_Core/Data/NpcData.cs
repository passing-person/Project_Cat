using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/Core/NPC Data")]
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

    public void NormalizeValues()
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            npcId = "Npc";
        }

        moveSpeed = Mathf.Max(0f, moveSpeed);
        chaseSpeed = Mathf.Max(0f, chaseSpeed);

        annoyedThreshold = Mathf.Clamp(annoyedThreshold, 0f, 100f);
        angryThreshold = Mathf.Clamp(angryThreshold, annoyedThreshold, 100f);
        enragedThreshold = Mathf.Clamp(enragedThreshold, angryThreshold, 100f);
    }

    public bool IsValid(out string message)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            message = "NpcData.npcId is empty.";
            return false;
        }

        if (annoyedThreshold > angryThreshold || angryThreshold > enragedThreshold)
        {
            message = "NpcData rage thresholds must be ordered: annoyed <= angry <= enraged.";
            return false;
        }

        message = "NpcData is valid.";
        return true;
    }

    private void OnValidate()
    {
        NormalizeValues();
    }
}
