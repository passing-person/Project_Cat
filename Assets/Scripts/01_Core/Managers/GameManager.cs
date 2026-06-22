using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public StageManager stageManager;
    public MonoBehaviour uiBridgeBehaviour;

    [Header("Debug")]
    [SerializeField] private GameState currentState = GameState.Boot;

    private ICoreUIBridge uiBridge;

    public GameState CurrentState => currentState;

    private void Awake()
    {
        ResolveUIBridge();
        InitializeGame();
    }

    public void InitializeGame()
    {
        SetGameState(GameState.Boot);
    }

    public void StartGame()
    {
        if (stageManager != null)
        {
            stageManager.LoadStage(stageManager.defaultStageData);
            stageManager.StartStage();
        }
        else
        {
            SetGameState(GameState.Playing);
        }
    }

    public void SetGameState(GameState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;
        Debug.Log($"GameState changed: {currentState}");
    }

    public void OnStageCleared(string stageId)
    {
        Debug.Log($"GameManager received stage clear: {stageId}");
    }

    public void OnStageFailed(string stageId, string reason)
    {
        Debug.Log($"GameManager received stage failure: {stageId}, reason: {reason}");
    }

    public void GameOver(string reason)
    {
        SetGameState(GameState.GameOver);

        if (uiBridge == null)
        {
            ResolveUIBridge();
        }

        if (uiBridge != null)
        {
            uiBridge.ShowGameOver(reason);
        }
    }

    public void RestartStage()
    {
        if (stageManager == null)
        {
            return;
        }

        stageManager.LoadStage(stageManager.CurrentStageData);
        stageManager.StartStage();
    }

    public void LoadNextStage()
    {
        Debug.Log("LoadNextStage is not implemented yet. Stage order is pending design confirmation.");
    }

    private void ResolveUIBridge()
    {
        uiBridge = uiBridgeBehaviour as ICoreUIBridge;
    }
}
