using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    [SerializeField] private string firstLevelSceneName = "Levels";

    public void GameStart()
    {
        Time.timeScale = 1f;
        GameInputGate.SetMenuOpen(false);
        SceneManager.LoadScene(firstLevelSceneName);
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
