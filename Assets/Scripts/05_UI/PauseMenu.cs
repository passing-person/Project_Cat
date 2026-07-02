using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;

    private bool isPaused = false;

    void Start()
    {
        pausePanel.SetActive(false);

        Time.timeScale = 1f;

        // 游戏开始隐藏鼠标并锁定
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    public void PauseGame()
    {
        pausePanel.SetActive(true);

        Time.timeScale = 0f;

        isPaused = true;

        // 显示鼠标
        Cursor.visible = true;

        // 解锁鼠标
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        pausePanel.SetActive(false);

        Time.timeScale = 1f;

        isPaused = false;

        // 隐藏鼠标
        Cursor.visible = false;

        // 锁定鼠标
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void GoToLevelSelect()
    {
        Time.timeScale = 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene(1);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene(0);
    }
}