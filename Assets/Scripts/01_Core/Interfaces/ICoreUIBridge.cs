public interface ICoreUIBridge
{
    void SetScore(int currentScore, int targetScore);
    void SetScoreMultiplier(float multiplier);
    void SetRage(string npcId, float currentRage, NpcRageState state);
    void SetTimer(float time);
    void SetObjectiveText(string text);
    void ShowPrompt(string message);
    void HidePrompt();
    void ShowDangerWarning(string message);
    void HideDangerWarning();
    void ShowStageClear();
    void ShowStageFailed(string reason);
    void ShowGameOver(string reason);
}
