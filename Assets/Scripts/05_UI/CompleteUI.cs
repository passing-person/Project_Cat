using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCompleteUI : MonoBehaviour
{
    public GameObject completePanel;

    public TMP_Text scoreText;

    void Start()
    {
        completePanel.SetActive(false);
    }

    // 显示结算界面
    public void ShowCompleteUI(int finalScore)
    {
        completePanel.SetActive(true);

        scoreText.text = finalScore.ToString();

        // 暂停游戏
        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // 返回关卡选择
    public void BackToLevelSelect()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene("Levels");
    }
}