using TMPro;
using UnityEngine;

public class DesignedUICoreConnector : MonoBehaviour, ICoreUIBridge
{
    [Header("Core")]
    [SerializeField] private CoreFacade coreFacade;

    [Header("Designed UI")]
    [SerializeField] private HUDManager hudManager;
    [SerializeField] private SkillCooldownUI skillCooldownUI;
    [SerializeField] private CompleteUI completeUI;
    [SerializeField] private PauseMenu pauseMenu;
    [SerializeField] private WorldRageBarManager worldRageBarManager;

    [Header("Optional Prompt UI")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private GameObject warningPanel;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text objectiveText;

    [Header("Legacy Feedback UI")]
    [SerializeField] private UIManager fallbackFeedbackUI;
    [SerializeField] private bool useFallbackFeedback = false;
    [SerializeField] private bool useFallbackHud = false;

    [Header("Polling")]
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private bool pollCore = true;
    [SerializeField] private float pollInterval = 0.05f;

    private float pollTimer;
    private bool resultShown;

    private void Awake()
    {
        useFallbackFeedback = false;
        useFallbackHud = false;
        LegacyUiCleanup.CleanupNow(gameObject);
        ResolveReferences();
        BindChildren();
        HideTransientPanels();
    }

    private void Start()
    {
        useFallbackFeedback = false;
        useFallbackHud = false;
        LegacyUiCleanup.CleanupNow(gameObject);
        ResolveReferences();
        BindChildren();
        if (coreFacade != null)
        {
            coreFacade.SetUIBridge(this);
        }
    }

    private void Update()
    {
        if (autoFindReferences)
        {
            ResolveReferences();
            BindChildren();
        }

        if (!pollCore)
        {
            return;
        }

        pollTimer -= Time.unscaledDeltaTime;
        if (pollTimer > 0f)
        {
            return;
        }

        pollTimer = Mathf.Max(0.02f, pollInterval);
        PollCore();
    }

    public void BindCore(CoreFacade facade)
    {
        coreFacade = facade;
        BindChildren();
        PollCore();
    }

    public void SetScore(int currentScore, int targetScore)
    {
        if (hudManager != null)
        {
            hudManager.SetScore(currentScore, targetScore);
        }

        if (useFallbackHud)
        {
            fallbackFeedbackUI?.SetScore(currentScore, targetScore);
        }
    }

    public void SetScoreMultiplier(float multiplier)
    {
        if (hudManager != null)
        {
            hudManager.SetMultiplier(multiplier);
        }

        if (useFallbackHud)
        {
            fallbackFeedbackUI?.SetScoreMultiplier(multiplier);
        }
    }

    public void SetRage(string npcId, float currentRage, NpcRageState state)
    {
        if (useFallbackHud)
        {
            fallbackFeedbackUI?.SetRage(npcId, currentRage, state);
        }
    }

    public void SetTimer(float time)
    {
        if (timerText != null)
        {
            timerText.text = time > 0f ? time.ToString("0.0") : string.Empty;
        }

        if (useFallbackFeedback)
        {
            fallbackFeedbackUI?.SetTimer(time);
        }
    }

    public void SetObjectiveText(string text)
    {
        if (objectiveText != null)
        {
            objectiveText.text = string.IsNullOrEmpty(text) ? string.Empty : text;
        }

        if (useFallbackFeedback)
        {
            fallbackFeedbackUI?.SetObjectiveText(text);
        }
    }

    public void ShowPrompt(string message)
    {
        if (promptText != null)
        {
            promptText.text = string.IsNullOrEmpty(message) ? string.Empty : message;
        }

        if (promptPanel != null)
        {
            promptPanel.SetActive(!string.IsNullOrEmpty(message));
        }

        if (useFallbackFeedback)
        {
            fallbackFeedbackUI?.ShowPrompt(message);
        }
    }

    public void HidePrompt()
    {
        if (promptText != null)
        {
            promptText.text = string.Empty;
        }

        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        if (useFallbackFeedback)
        {
            fallbackFeedbackUI?.HidePrompt();
        }
    }

    public void ShowDangerWarning(string message)
    {
        if (warningText != null)
        {
            warningText.text = string.IsNullOrEmpty(message) ? string.Empty : message;
        }

        if (warningPanel != null)
        {
            warningPanel.SetActive(!string.IsNullOrEmpty(message));
        }

        if (useFallbackFeedback)
        {
            fallbackFeedbackUI?.ShowDangerWarning(message);
        }
    }

    public void HideDangerWarning()
    {
        if (warningText != null)
        {
            warningText.text = string.Empty;
        }

        if (warningPanel != null)
        {
            warningPanel.SetActive(false);
        }

        if (useFallbackFeedback)
        {
            fallbackFeedbackUI?.HideDangerWarning();
        }
    }

    public void ShowStageClear()
    {
        resultShown = true;
        int score = coreFacade != null ? coreFacade.CurrentScore : 0;
        if (completeUI != null)
        {
            completeUI.ShowCompleteUI(score);
        }

        if (useFallbackFeedback)
        {
            fallbackFeedbackUI?.ShowStageClear();
        }
    }

    public void ShowStageFailed(string reason)
    {
        resultShown = true;
        int score = coreFacade != null ? coreFacade.CurrentScore : 0;
        if (completeUI != null)
        {
            completeUI.ShowFailUI(score, reason);
        }

        if (useFallbackFeedback)
        {
            fallbackFeedbackUI?.ShowStageFailed(reason);
        }
    }

    public void ShowGameOver(string reason)
    {
        ShowStageFailed(reason);
    }

    private void PollCore()
    {
        if (coreFacade == null)
        {
            return;
        }

        SetScore(coreFacade.CurrentScore, coreFacade.TargetScore);
        SetScoreMultiplier(coreFacade.CurrentMultiplier);

        if (!resultShown)
        {
            if (coreFacade.StageCleared)
            {
                ShowStageClear();
            }
            else if (coreFacade.StageFailed)
            {
                ShowStageFailed("Caught before target score");
            }
        }
    }

    private void ResolveReferences()
    {
        if (coreFacade == null) coreFacade = FindObjectOfType<CoreFacade>();
        if (hudManager == null) hudManager = GetComponentInChildren<HUDManager>(true) ?? FindObjectOfType<HUDManager>(true);
        if (skillCooldownUI == null) skillCooldownUI = GetComponentInChildren<SkillCooldownUI>(true) ?? FindObjectOfType<SkillCooldownUI>(true);
        if (completeUI == null) completeUI = GetComponentInChildren<CompleteUI>(true) ?? FindObjectOfType<CompleteUI>(true);
        if (pauseMenu == null) pauseMenu = GetComponentInChildren<PauseMenu>(true) ?? FindObjectOfType<PauseMenu>(true);
        if (worldRageBarManager == null) worldRageBarManager = GetComponentInChildren<WorldRageBarManager>(true);
        if (worldRageBarManager == null) worldRageBarManager = gameObject.AddComponent<WorldRageBarManager>();
        if (fallbackFeedbackUI == null) fallbackFeedbackUI = FindObjectOfType<UIManager>();
    }

    private void BindChildren()
    {
        if (hudManager != null)
        {
            hudManager.BindCore(coreFacade);
        }

        if (completeUI != null)
        {
            completeUI.BindCore(coreFacade);
        }

        if (worldRageBarManager != null)
        {
            worldRageBarManager.BindCore(coreFacade);
            worldRageBarManager.SetVisible(true);
        }

        if (fallbackFeedbackUI != null)
        {
            fallbackFeedbackUI.DisableImmediateModeOverlay();
        }
    }

    private void HideTransientPanels()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
        if (warningPanel != null) warningPanel.SetActive(false);
    }
}
