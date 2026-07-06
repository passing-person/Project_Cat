using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelComplete : MonoBehaviour
{
    public int currentLevel;
    [SerializeField] private string levelScenePrefix = "Level";

    public void UnlockNextLevel()
    {
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel", 0);
        if (currentLevel >= unlockedLevel)
        {
            PlayerPrefs.SetInt("UnlockedLevel", currentLevel + 1);
            PlayerPrefs.Save();
        }
    }

    public void LoadNextLevel()
    {
        string nextScene = levelScenePrefix + (currentLevel + 1);
        Time.timeScale = 1f;
        GameInputGate.SetMenuOpen(false);
        SceneManager.LoadScene(nextScene);
    }

    public void CompleteLevel()
    {
        UnlockNextLevel();
        LoadNextLevel();
    }
}
