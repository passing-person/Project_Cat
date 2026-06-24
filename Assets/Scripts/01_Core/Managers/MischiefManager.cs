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

    [Header("Target State")]
    public bool autoTickTargetCooldowns = true;

    private readonly HashSet<string> lockedTargets = new HashSet<string>();
    private readonly Dictionary<string, MischiefTargetState> targetStates = new Dictionary<string, MischiefTargetState>();
    private readonly Dictionary<string, float> targetCooldownRemaining = new Dictionary<string, float>();
    private ICoreUIBridge uiBridge;

    private void Awake()
    {
        ResolveUIBridge();
    }

    private void Update()
    {
        if (autoTickTargetCooldowns)
        {
            TickTargetCooldowns(Time.deltaTime);
        }
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

        return GetMischiefTargetState(targetId) == MischiefTargetState.Available;
    }

    public MischiefTargetState GetMischiefTargetState(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return MischiefTargetState.Disabled;
        }

        if (lockedTargets.Contains(targetId))
        {
            return MischiefTargetState.Locked;
        }

        if (targetCooldownRemaining.TryGetValue(targetId, out float remaining) && remaining > 0f)
        {
            return MischiefTargetState.Cooldown;
        }

        if (targetStates.TryGetValue(targetId, out MischiefTargetState state))
        {
            return state;
        }

        return MischiefTargetState.Available;
    }

    public void SetMischiefTargetState(string targetId, MischiefTargetState state)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return;
        }

        targetStates[targetId] = state;

        if (state == MischiefTargetState.Locked)
        {
            lockedTargets.Add(targetId);
            targetCooldownRemaining.Remove(targetId);
            return;
        }

        lockedTargets.Remove(targetId);

        if (state != MischiefTargetState.Cooldown)
        {
            targetCooldownRemaining.Remove(targetId);
        }
    }

    public void StartMischiefTargetCooldown(string targetId, float duration)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return;
        }

        if (duration <= 0f)
        {
            SetMischiefTargetState(targetId, MischiefTargetState.Available);
            return;
        }

        lockedTargets.Remove(targetId);
        targetStates[targetId] = MischiefTargetState.Cooldown;
        targetCooldownRemaining[targetId] = duration;
    }

    public void DisableMischiefTarget(string targetId)
    {
        SetMischiefTargetState(targetId, MischiefTargetState.Disabled);
    }

    public float GetTargetCooldownRemaining(string targetId)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            return 0f;
        }

        if (targetCooldownRemaining.TryGetValue(targetId, out float remaining))
        {
            return Mathf.Max(0f, remaining);
        }

        return 0f;
    }

    public void TickTargetCooldowns(float deltaTime)
    {
        if (deltaTime <= 0f || targetCooldownRemaining.Count == 0)
        {
            return;
        }

        List<string> completedTargets = null;
        List<string> targetIds = new List<string>(targetCooldownRemaining.Keys);

        for (int i = 0; i < targetIds.Count; i++)
        {
            string targetId = targetIds[i];
            float remaining = targetCooldownRemaining[targetId] - deltaTime;
            targetCooldownRemaining[targetId] = remaining;

            if (remaining <= 0f)
            {
                if (completedTargets == null)
                {
                    completedTargets = new List<string>();
                }

                completedTargets.Add(targetId);
            }
        }

        if (completedTargets == null)
        {
            return;
        }

        for (int i = 0; i < completedTargets.Count; i++)
        {
            string targetId = completedTargets[i];
            targetCooldownRemaining.Remove(targetId);

            if (targetStates.TryGetValue(targetId, out MischiefTargetState state) && state == MischiefTargetState.Cooldown)
            {
                targetStates[targetId] = MischiefTargetState.Available;
            }
        }
    }

    public void LockMischiefTarget(string targetId)
    {
        SetMischiefTargetState(targetId, MischiefTargetState.Locked);
    }

    public void UnlockMischiefTarget(string targetId)
    {
        SetMischiefTargetState(targetId, MischiefTargetState.Available);
    }

    public bool IsTargetLocked(string targetId)
    {
        return GetMischiefTargetState(targetId) == MischiefTargetState.Locked;
    }

    public void ResetTargetStates()
    {
        lockedTargets.Clear();
        targetStates.Clear();
        targetCooldownRemaining.Clear();
    }

    private void ResolveUIBridge()
    {
        uiBridge = uiBridgeBehaviour as ICoreUIBridge;
    }
}
