using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;

    [Header("Scene Names")]
    [SerializeField] private string levelSelectSceneName = "Levels";
    [SerializeField] private string mainMenuSceneName = "TitleScene";

    private bool isPaused;
    private float previousTimeScale = 1f;
    private bool initialized;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        // This call is safe: it only disables old generated UI and never disables MainCanvas.
        MainCanvasUiRepair.Repair(gameObject);
    }

    private void Start()
    {
        EnsureInitialState();
    }

    private void OnEnable()
    {
        if (initialized)
        {
            return;
        }

        EnsureInitialState();
    }

    private void Update()
    {
        if (!MainCanvasUiRepair.IsUnderMainCanvas(gameObject))
        {
            // Legacy PauseMenu outside MainCanvas should not process ESC.
            return;
        }

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

    public void EnsureInitialState()
    {
        initialized = true;
        isPaused = false;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (Time.timeScale <= 0f)
        {
            Time.timeScale = 1f;
        }

        GameInputGate.SetMenuOpen(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void PauseGame()
    {
        if (!MainCanvasUiRepair.IsUnderMainCanvas(gameObject))
        {
            return;
        }

        if (isPaused)
        {
            return;
        }

        previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
        Time.timeScale = 0f;
        isPaused = true;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }

        GameInputGate.SetMenuOpen(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ResumeGame()
    {
        if (!isPaused)
        {
            return;
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        isPaused = false;
        GameInputGate.SetMenuOpen(false);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        isPaused = false;
        GameInputGate.SetMenuOpen(false);
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    public void GoToLevelSelect()
    {
        Time.timeScale = 1f;
        isPaused = false;
        GameInputGate.SetMenuOpen(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        isPaused = false;
        GameInputGate.SetMenuOpen(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
