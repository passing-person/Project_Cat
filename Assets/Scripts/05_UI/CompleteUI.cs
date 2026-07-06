using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CompleteUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject completePanel;
    public TMP_Text scoreText;
    public TMP_Text titleText;
    public TMP_Text detailText;

    [Header("Core Binding")]
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private bool autoFindCore = true;
    [SerializeField] private bool autoShowFromCore = true;
    [SerializeField] private string levelSelectSceneName = "Levels";

    private bool hasShownResult;

    private void Start()
    {
        ResolveCore();

        if (completePanel != null)
        {
            completePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!autoShowFromCore || hasShownResult)
        {
            return;
        }

        ResolveCore();
        if (coreFacade == null)
        {
            return;
        }

        if (coreFacade.StageCleared)
        {
            ShowCompleteUI(coreFacade.CurrentScore);
        }
        else if (coreFacade.StageFailed)
        {
            ShowFailUI(coreFacade.CurrentScore, "Caught before target score");
        }
    }

    public void BindCore(CoreFacade facade)
    {
        coreFacade = facade;
    }

    public void ShowCompleteUI(int finalScore)
    {
        ShowResult("CLEAR", finalScore, "Stage Complete");
    }

    public void ShowCompleteUI()
    {
        int score = coreFacade != null ? coreFacade.CurrentScore : 0;
        ShowCompleteUI(score);
    }

    public void ShowFailUI(int finalScore, string reason)
    {
        ShowResult("FAIL", finalScore, string.IsNullOrEmpty(reason) ? "Stage Failed" : reason);
    }

    public void ShowFailUI(string reason)
    {
        int score = coreFacade != null ? coreFacade.CurrentScore : 0;
        ShowFailUI(score, reason);
    }

    public void BackToLevelSelect()
    {
        Time.timeScale = 1f;
        GameInputGate.SetMenuOpen(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (!string.IsNullOrEmpty(levelSelectSceneName))
        {
            SceneManager.LoadScene(levelSelectSceneName);
        }
    }

    public void RetryCurrentScene()
    {
        Time.timeScale = 1f;
        GameInputGate.SetMenuOpen(false);
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    public void HideCompleteUI()
    {
        hasShownResult = false;

        if (completePanel != null)
        {
            completePanel.SetActive(false);
        }

        GameInputGate.SetMenuOpen(false);
    }

    private void ShowResult(string title, int finalScore, string detail)
    {
        hasShownResult = true;

        if (completePanel != null)
        {
            completePanel.SetActive(true);
        }

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (scoreText != null)
        {
            scoreText.text = finalScore.ToString();
        }

        if (detailText != null)
        {
            detailText.text = detail;
        }

        Time.timeScale = 0f;
        GameInputGate.SetMenuOpen(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ResolveCore()
    {
        if (!autoFindCore || coreFacade != null)
        {
            return;
        }

        coreFacade = FindObjectOfType<CoreFacade>();
    }
}
