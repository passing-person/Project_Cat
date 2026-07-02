using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LevelSelectManager : MonoBehaviour
{
    //Five level buttons in total
    public Button[] levelButtons;

    //Color change
    public Color unlockedColor = Color.white;
    public Color lockedColor = Color.gray;

    void Start()
    {
        //Enter this game for the 1st time
        if (!PlayerPrefs.HasKey("UnlockedLevel"))
        {
            PlayerPrefs.SetInt("UnlockedLevel", 0);
            PlayerPrefs.Save();
        }

        UpdateButtons();
    }

    void UpdateButtons()
    {
        int unlockedLevel = PlayerPrefs.GetInt("UnlockedLevel");

        for (int i = 0; i < levelButtons.Length; i++)
        {
            Image img = levelButtons[i].GetComponent<Image>();

            if (i <= unlockedLevel)
            {
                levelButtons[i].interactable = true;
                img.color = unlockedColor;
            }
            else
            {
                levelButtons[i].interactable = false;
                img.color = lockedColor;
            }
        }
    }

    public void LoadLevel(int levelIndex)
    {
        SceneManager.LoadScene("Level" + levelIndex);
    }
}