using System.Collections.Generic;
using UnityEngine;

public class MischiefManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private RageManager rageManager;
    [SerializeField] private ObjectiveManager objectiveManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private AudioManager audioManager;

    private readonly HashSet<string> lockedTargetIds = new HashSet<string>();

    public void ApplyMischief(MischiefContext context)
    {
        if (!CanApplyMischief(context.TargetId))
            return;

        if (rageManager != null)
            rageManager.AddRageByMischief(context);

        if (scoreManager != null)
        {
            scoreManager.StartScoring();

            // Instant score is optional. Keep most normal target data at 0 if using time-based score only.
            if (context.BaseScore > 0)
                scoreManager.AddScore(context.BaseScore);
        }

        if (objectiveManager != null)
            objectiveManager.UpdateObjectiveProgress();

        if (audioManager != null)
            audioManager.PlaySfxAt(context.MischiefType.ToString(), context.Position);
    }

    public bool CanApplyMischief(string targetId)
    {
        if (string.IsNullOrEmpty(targetId))
            return false;

        return !lockedTargetIds.Contains(targetId);
    }

    public void LockMischiefTarget(string targetId)
    {
        if (!string.IsNullOrEmpty(targetId))
            lockedTargetIds.Add(targetId);
    }

    public void UnlockMischiefTarget(string targetId)
    {
        if (!string.IsNullOrEmpty(targetId))
            lockedTargetIds.Remove(targetId);
    }
}
