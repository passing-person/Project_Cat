using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameState CurrentState { get; private set; } = GameState.Boot;

    [Header("Managers")]
    [SerializeField] private StageManager stageManager;
    [SerializeField] private UIManager uiManager;

    [Header("Stage")]
    [SerializeField] private StageData firstStageData;

    public void InitializeGame()
    {
        SetGameState(GameState.Boot);
    }

    public void StartGame()
    {
        if (stageManager != null && firstStageData != null)
        {
            stageManager.LoadStage(firstStageData);
            stageManager.StartStage();
        }
        else
        {
            SetGameState(GameState.Playing);
        }
    }

    public void SetGameState(GameState newState)
    {
        CurrentState = newState;

        // UI can react to state here later if needed.
    }

    public void OnStageCleared(string stageId)
    {
        SetGameState(GameState.StageClear);

        if (uiManager != null)
            uiManager.ShowStageClear();
    }

    public void OnStageFailed(string stageId, string reason)
    {
        SetGameState(GameState.StageFailed);

        if (uiManager != null)
            uiManager.ShowStageFailed(reason);
    }

    public void GameOver(string reason)
    {
        SetGameState(GameState.GameOver);

        if (uiManager != null)
            uiManager.ShowGameOver(reason);
    }

    public void RestartStage()
    {
        // TODO: Reload scene or reset current stage objects.
    }

    public void LoadNextStage()
    {
        // TODO: Load next stage using CurrentStageData.nextStageId.
    }
}
