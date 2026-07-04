using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CompleteUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject completePanel;

    public TMP_Text scoreText;

    private void Start()
    {
        if (completePanel != null)
        {
            completePanel.SetActive(false);
        }
    }

    /// <summary>
    /// 显示通关结算界面
    /// </summary>
    public void ShowCompleteUI(int finalScore)
    {
        if (completePanel != null)
        {
            completePanel.SetActive(true);
        }

        if (scoreText != null)
        {
            scoreText.text = finalScore.ToString();
        }

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// 不传分数的版本
    /// 给当前 ShowStageClear() 兼容使用
    /// </summary>
    public void ShowCompleteUI()
    {
        if (completePanel != null)
        {
            completePanel.SetActive(true);
        }

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// 返回关卡选择界面
    /// </summary>
    public void BackToLevelSelect()
    {
        Time.timeScale = 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene("Levels");
    }

    /// <summary>
    /// 关闭结算界面
    /// </summary>
    public void HideCompleteUI()
    {
        if (completePanel != null)
        {
            completePanel.SetActive(false);
        }
    }
}