using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelectManager : MonoBehaviour
{
    public Button[] levelButtons;
    public Color unlockedColor = Color.white;
    public Color lockedColor = Color.gray;
    [SerializeField] private string levelScenePrefix = "Level";

    private void Start()
    {
        if (!PlayerPrefs.HasKey("UnlockedLevel"))
        {
            PlayerPrefs.SetInt("UnlockedLevel", 0);
            PlayerPrefs.Save();
        }

        UpdateButtons();
    }

    private void UpdateButtons()
    {
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel");

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] == null)
            {
                continue;
            }

            Image image = levelButtons[i].GetComponent<Image>();
            bool unlocked = i <= unlockedLevel;
            levelButtons[i].interactable = unlocked;

            if (image != null)
            {
                image.color = unlocked ? unlockedColor : lockedColor;
            }
        }
    }

    public void LoadLevel(int levelIndex)
    {
        Time.timeScale = 1f;
        GameInputGate.SetMenuOpen(false);
        // SceneManager.LoadScene(levelScenePrefix + levelIndex);
        SceneManager.LoadScene(levelScenePrefix + 2);
    }
}
