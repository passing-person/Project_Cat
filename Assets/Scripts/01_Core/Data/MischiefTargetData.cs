using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/Core/Mischief Target Data")]
public class MischiefTargetData : ScriptableObject
{
    [Header("Identity")]
    public string targetId = "Keyboard";
    public MischiefType mischiefType = MischiefType.Stomp;

    [Header("Score")]
    public int instantScoreBonus = 0;

    [Header("Rage")]
    public float baseRageAmount = 5f;
    public float rageRadius = 5f;
    public string primaryNpcId = "Supervisor";

    [Header("Lock Rule")]
    public bool canBeLocked = true;
    public float lockAtRageThreshold = 70f;

    // Compatibility aliases for newer Core code.
    public int InstantScoreBonus => instantScoreBonus;
    public float BaseRageAmount => baseRageAmount;
    public bool CanBeLocked => canBeLocked;
    public float LockAtRageThreshold => lockAtRageThreshold;

    public void NormalizeValues()
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            targetId = "MischiefTarget";
        }

        instantScoreBonus = Mathf.Max(0, instantScoreBonus);
        baseRageAmount = Mathf.Max(0f, baseRageAmount);
        rageRadius = Mathf.Max(0f, rageRadius);
        lockAtRageThreshold = Mathf.Clamp(lockAtRageThreshold, 0f, 100f);
    }

    public bool IsValid(out string message)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            message = "MischiefTargetData.targetId is empty.";
            return false;
        }

        if (baseRageAmount < 0f)
        {
            message = "MischiefTargetData.baseRageAmount cannot be negative.";
            return false;
        }

        if (rageRadius < 0f)
        {
            message = "MischiefTargetData.rageRadius cannot be negative.";
            return false;
        }

        message = "MischiefTargetData is valid.";
        return true;
    }

    private void OnValidate()
    {
        NormalizeValues();
    }
}
