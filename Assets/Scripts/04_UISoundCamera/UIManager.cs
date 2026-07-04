using UnityEngine;

public class UIManager : MonoBehaviour
{
    public void ShowPrompt(string message)
    {
        Debug.Log("Prompt:\n" + message);
    }

    public void HidePrompt()
    {
        Debug.Log(BilingualDebug.Line("提示已隐藏", "Prompt hidden"));
    }

    public void SetObjectiveText(string text)
    {
        Debug.Log("Objective: " + text);
    }

    public void SetScore(int currentScore, int targetScore)
    {
        Debug.Log("Score: " + currentScore + " / " + targetScore);
    }

    public void SetScoreMultiplier(float multiplier)
    {
        Debug.Log("Score Multiplier: " + multiplier.ToString("0.00"));
    }

    public void SetRage(string npcId, float currentRage, NpcRageState state)
    {
        Debug.Log("Rage: " + npcId + " = " + currentRage.ToString("0") + "% / " + state);
    }

    public void SetTimer(float time)
    {
        Debug.Log("Timer: " + time.ToString("0.0"));
    }

    public void SetCuteCooldown(float remainingSeconds, float totalSeconds)
    {
        Debug.Log(BilingualDebug.Line(
            $"撒娇冷却: {Mathf.Max(0f, remainingSeconds):0.0}s / {totalSeconds:0.0}s",
            $"Cute cooldown: {Mathf.Max(0f, remainingSeconds):0.0}s / {totalSeconds:0.0}s"));
    }

    public void HideCuteCooldown()
    {
        Debug.Log(BilingualDebug.Line("撒娇冷却: 就绪", "Cute cooldown: ready"));
    }

    public void ShowDangerWarning(string message)
    {
        Debug.Log("Danger: " + message);
    }

    public void HideDangerWarning()
    {
        Debug.Log("Danger hidden");
    }

    public void ShowStageClear()
    {
        Debug.Log("Stage Clear");
    }

    public void ShowStageFailed(string reason)
    {
        Debug.Log("Stage Failed: " + reason);
    }

    public void ShowGameOver(string reason)
    {
        Debug.Log("Game Over: " + reason);
    }
}
