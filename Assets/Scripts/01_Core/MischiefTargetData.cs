using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/Mischief Target Data")]
public class MischiefTargetData : ScriptableObject
{
    [Header("Identity")]
    public string targetId = "Target";
    public MischiefType mischiefType = MischiefType.Stomp;

    [Header("Score")]
    public int instantScoreBonus = 0;

    [Header("Rage")]
    public float baseRageAmount = 5f;
    public float rageRadius = 5f;
    public string primaryNpcId = "";

    [Header("Lock Rule")]
    public bool canBeLocked = false;
    public float lockAtRageThreshold = 70f;
}
