using UnityEngine;

public class CoreUIBridge : MonoBehaviour, ICoreUIBridge
{
    [Header("HUD")]
    [SerializeField] private HUDManager hudManager;

    [Header("Stage Result")]
    [SerializeField] private CompleteUI completeUI;

    [Header("Prompt")]
    [SerializeField] private GameObject promptPanel;

    [SerializeField] private TMPro.TMP_Text promptText;

    [Header("Danger Warning")]
    [SerializeField] private GameObject warningPanel;

    [SerializeField] private TMPro.TMP_Text warningText;

    [Header("Timer")]
    [SerializeField] private TMPro.TMP_Text timerText;

    [Header("Objective")]
    [SerializeField] private TMPro.TMP_Text objectiveText;

    [Header("Core")]
    [SerializeField] private CoreFacade coreFacade;

    private void Awake()
    {
        HidePrompt();
        HideDangerWarning();

        if (completeUI != null)
        {
            completeUI.HideCompleteUI();
        }
    }

    private void Start()
    {
        if (coreFacade == null)
        {
            coreFacade = FindObjectOfType<CoreFacade>();
        }

        if (coreFacade != null)
        {
            coreFacade.SetUIBridge(this);
        }
        else
        {
            Debug.LogWarning("CoreFacade not found.");
        }
    }
    #region Score

    public void SetScore(int currentScore, int targetScore)
    {
        Debug.Log($"Bridge Score={currentScore}, Target={targetScore}");
        if (hudManager != null)
        {
            hudManager.SetScore(currentScore, targetScore);
        }
    }

    public void SetScoreMultiplier(float multiplier)
    {
        if (hudManager != null)
        {

            hudManager.SetMultiplier(multiplier);
        }
    }

    #endregion

    #region Rage

    public void SetRage(string npcId, float currentRage, NpcRageState state)
    {
        // Rage UI以后再接
        Debug.Log($"Rage | {npcId} : {currentRage} ({state})");
    }

    #endregion

    #region Timer

    public void SetTimer(float time)
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(time).ToString();
        }
    }

    #endregion

    #region Objective

    public void SetObjectiveText(string text)
    {
        if (objectiveText != null)
        {
            objectiveText.text = text;
        }
    }

    #endregion

    #region Prompt

    public void ShowPrompt(string message)
    {
        if (promptPanel != null)
            promptPanel.SetActive(true);

        if (promptText != null)
            promptText.text = message;
    }

    public void HidePrompt()
    {
        if (promptPanel != null)
            promptPanel.SetActive(false);
    }

    #endregion

    #region Warning

    public void ShowDangerWarning(string message)
    {
        if (warningPanel != null)
            warningPanel.SetActive(true);

        if (warningText != null)
            warningText.text = message;
    }

    public void HideDangerWarning()
    {
        if (warningPanel != null)
            warningPanel.SetActive(false);
    }

    #endregion

    #region Result

    public void ShowStageClear()
    {
        if (completeUI != null)
        {
            completeUI.ShowCompleteUI();
        }
    }

    public void ShowStageFailed(string reason)
    {
        Debug.Log("Stage Failed : " + reason);

        // 以后接失败UI
    }

    public void ShowGameOver(string reason)
    {
        Debug.Log("Game Over : " + reason);

        // 以后接GameOver UI
    }

    #endregion
}

