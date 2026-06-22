using UnityEngine;

public class MockUIBridge : MonoBehaviour, ICoreUIBridge
{
    [Header("Debug Values")]
    public int lastScore;
    public int lastTargetScore;
    public float lastMultiplier;
    public string lastRageNpcId;
    public float lastRage;
    public NpcRageState lastRageState;
    public float lastTimer;
    public string lastObjectiveText;
    public string lastPrompt;
    public string lastFailureReason;
    public bool stageClearShown;
    public bool stageFailedShown;
    public bool gameOverShown;

    public void SetScore(int currentScore, int targetScore)
    {
        lastScore = currentScore;
        lastTargetScore = targetScore;
        Debug.Log($"[MockUI] Score {currentScore}/{targetScore}");
    }

    public void SetScoreMultiplier(float multiplier)
    {
        lastMultiplier = multiplier;
        Debug.Log($"[MockUI] Multiplier {multiplier:0.00}");
    }

    public void SetRage(string npcId, float currentRage, NpcRageState state)
    {
        lastRageNpcId = npcId;
        lastRage = currentRage;
        lastRageState = state;
        Debug.Log($"[MockUI] Rage {npcId}: {currentRage:0.0}, {state}");
    }

    public void SetTimer(float time)
    {
        lastTimer = time;
        Debug.Log($"[MockUI] Timer {time:0.0}");
    }

    public void SetObjectiveText(string text)
    {
        lastObjectiveText = text;
        Debug.Log($"[MockUI] Objective {text}");
    }

    public void ShowPrompt(string message)
    {
        lastPrompt = message;
        Debug.Log($"[MockUI] Prompt {message}");
    }

    public void HidePrompt()
    {
        lastPrompt = string.Empty;
    }

    public void ShowDangerWarning(string message)
    {
        Debug.Log($"[MockUI] Danger {message}");
    }

    public void HideDangerWarning()
    {
        Debug.Log("[MockUI] Hide danger warning.");
    }

    public void ShowStageClear()
    {
        stageClearShown = true;
        Debug.Log("[MockUI] Stage Clear");
    }

    public void ShowStageFailed(string reason)
    {
        stageFailedShown = true;
        lastFailureReason = reason;
        Debug.Log($"[MockUI] Stage Failed: {reason}");
    }

    public void ShowGameOver(string reason)
    {
        gameOverShown = true;
        Debug.Log($"[MockUI] Game Over: {reason}");
    }

    public void ResetMockValues()
    {
        lastScore = 0;
        lastTargetScore = 0;
        lastMultiplier = 0f;
        lastRageNpcId = string.Empty;
        lastRage = 0f;
        lastRageState = NpcRageState.Calm;
        lastTimer = 0f;
        lastObjectiveText = string.Empty;
        lastPrompt = string.Empty;
        lastFailureReason = string.Empty;
        stageClearShown = false;
        stageFailedShown = false;
        gameOverShown = false;
    }
}
