using System.Collections.Generic;
using UnityEngine;

public class MischiefManager : MonoBehaviour
{
    [Header("References")]
    public RageManager rageManager;
    public ScoreManager scoreManager;
    public ObjectiveManager objectiveManager;
    public MonoBehaviour uiBridgeBehaviour;

    [Header("Score")]
    public bool grantInstantScoreFromContext = false;

    private readonly HashSet<string> lockedTargets = new HashSet<string>();
    private ICoreUIBridge uiBridge;

    private void Awake()
    {
        ResolveUIBridge();
    }

    public bool ApplyMischief(MischiefContext context)
    {
        if (!CanApplyMischief(context.TargetId))
        {
            Debug.Log($"Mischief blocked: {context.TargetId}");
            return false;
        }

        if (rageManager != null)
        {
            rageManager.AddRageByMischief(context);
        }

        if (scoreManager != null)
        {
            scoreManager.StartScoring();

            if (grantInstantScoreFromContext && context.BaseScore > 0)
            {
                scoreManager.AddScore(context.BaseScore);
            }
        }

        if (objectiveManager != null)
        {
            objectiveManager.UpdateObjectiveProgress();
        }

        if (uiBridge == null)
        {
            ResolveUIBridge();
        }

        if (uiBridge != null)
        {
            uiBridge.ShowPrompt($"Mischief: {context.TargetId}");
        }

        return true;
    }

    public bool CanApplyMischief(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return false;
        }

        return !lockedTargets.Contains(targetId);
    }

    public void LockMischiefTarget(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return;
        }

        lockedTargets.Add(targetId);
    }

    public void UnlockMischiefTarget(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return;
        }

        lockedTargets.Remove(targetId);
    }

    public bool IsTargetLocked(string targetId)
    {
        return !string.IsNullOrWhiteSpace(targetId) && lockedTargets.Contains(targetId);
    }

    private void ResolveUIBridge()
    {
        uiBridge = uiBridgeBehaviour as ICoreUIBridge;
    }
}
