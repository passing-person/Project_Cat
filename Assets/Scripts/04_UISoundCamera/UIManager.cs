using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour, ICoreUIBridge
{
    [Header("References")]
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text multiplierText;
    [SerializeField] private Text rageText;
    [SerializeField] private Text objectiveText;
    [SerializeField] private Text promptText;
    [SerializeField] private Text timerText;
    [SerializeField] private Text cooldownText;
    [SerializeField] private Text dangerText;
    [SerializeField] private Text resultText;
    [SerializeField] private Text interactionText;
    [SerializeField] private Text controlsText;
    [SerializeField] private Text feedbackText;
    [SerializeField] private Text hiddenText;
    [SerializeField] private Image scoreFill;
    [SerializeField] private Image rageFill;
    [SerializeField] private Image multiplierFill;
    [SerializeField] private Image hideOverlay;
    [SerializeField] private Image promptPanel;
    [SerializeField] private Image dangerPanel;

    [Header("Options")]
    [SerializeField] private bool autoCreateUI = true;
    [SerializeField] private bool autoFindCoreFacade = true;
    [SerializeField] private string watchedNpcId = "Supervisor";
    [SerializeField] private float pollInterval = 0.1f;
    [SerializeField] private float feedbackMessageDuration = 1.5f;

    private float pollTimer;
    private float feedbackTimer;
    private int lastScore;
    private int lastTargetScore;
    private float lastMultiplier = 1f;
    private float lastRage;
    private NpcRageState lastRageState = NpcRageState.Calm;
    private string lastObjective = string.Empty;
    private string lastPrompt = string.Empty;
    private string lastDanger = string.Empty;
    private bool hasResult;
    private bool hiddenVisualActive;

    private readonly Color panelColor = new Color(0f, 0f, 0f, 0.55f);
    private readonly Color scoreColor = new Color(0.1f, 0.85f, 0.35f, 0.95f);
    private readonly Color multiplierColor = new Color(0.2f, 0.55f, 1f, 0.95f);
    private readonly Color rageCalmColor = new Color(0.3f, 0.85f, 0.35f, 0.95f);
    private readonly Color rageAngryColor = new Color(1f, 0.65f, 0.1f, 0.95f);
    private readonly Color rageMaxColor = new Color(1f, 0.1f, 0.05f, 0.95f);

    private void Awake()
    {
        if (autoCreateUI)
        {
            EnsureUI();
        }

        if (autoFindCoreFacade && coreFacade == null)
        {
            coreFacade = FindObjectOfType<CoreFacade>();
        }
    }

    private void Start()
    {
        if (coreFacade != null)
        {
            coreFacade.SetUIBridge(this);
        }

        RefreshAllText();
    }

    private void Update()
    {
        if (autoFindCoreFacade && coreFacade == null)
        {
            coreFacade = FindObjectOfType<CoreFacade>();
            if (coreFacade != null)
            {
                coreFacade.SetUIBridge(this);
            }
        }

        TickFeedbackMessage();

        pollTimer -= Time.deltaTime;
        if (pollTimer > 0f)
        {
            return;
        }

        pollTimer = Mathf.Max(0.02f, pollInterval);
        PollCoreState();
    }

    public void BindCore(CoreFacade facade)
    {
        coreFacade = facade;
        if (coreFacade != null)
        {
            coreFacade.SetUIBridge(this);
        }
    }

    public void SetWatchedNpcId(string npcId)
    {
        if (!string.IsNullOrWhiteSpace(npcId))
        {
            watchedNpcId = npcId;
        }
    }

    public void SetScore(int currentScore, int targetScore)
    {
        lastScore = Mathf.Max(0, currentScore);
        lastTargetScore = Mathf.Max(0, targetScore);
        UpdateScoreText();
    }

    public void SetScoreMultiplier(float multiplier)
    {
        lastMultiplier = Mathf.Max(0f, multiplier);
        UpdateMultiplierText();
    }

    public void SetRage(string npcId, float currentRage, NpcRageState state)
    {
        if (!string.IsNullOrWhiteSpace(npcId))
        {
            watchedNpcId = npcId;
        }

        lastRage = Mathf.Clamp(currentRage, 0f, 100f);
        lastRageState = state;
        UpdateRageText();
    }

    public void SetTimer(float time)
    {
        if (timerText == null)
        {
            return;
        }

        if (float.IsInfinity(time) || time <= 0f)
        {
            timerText.text = string.Empty;
            return;
        }

        timerText.text = "Hide Timer: " + time.ToString("0.0") + "s";
    }

    public void SetObjectiveText(string text)
    {
        lastObjective = string.IsNullOrWhiteSpace(text) ? string.Empty : text;
        if (objectiveText != null)
        {
            objectiveText.text = lastObjective;
        }
    }

    public void ShowPrompt(string message)
    {
        lastPrompt = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
        if (promptText != null)
        {
            promptText.text = lastPrompt;
            promptText.enabled = !string.IsNullOrEmpty(lastPrompt);
        }

        if (promptPanel != null)
        {
            promptPanel.enabled = !string.IsNullOrEmpty(lastPrompt);
        }
    }

    public void HidePrompt()
    {
        lastPrompt = string.Empty;
        if (promptText != null)
        {
            promptText.text = string.Empty;
            promptText.enabled = false;
        }

        if (promptPanel != null)
        {
            promptPanel.enabled = false;
        }
    }

    public void ShowDangerWarning(string message)
    {
        lastDanger = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
        if (dangerText != null)
        {
            dangerText.text = lastDanger;
            dangerText.enabled = !string.IsNullOrEmpty(lastDanger);
        }

        if (dangerPanel != null)
        {
            dangerPanel.enabled = !string.IsNullOrEmpty(lastDanger);
        }
    }

    public void HideDangerWarning()
    {
        lastDanger = string.Empty;
        if (dangerText != null)
        {
            dangerText.text = string.Empty;
            dangerText.enabled = false;
        }

        if (dangerPanel != null)
        {
            dangerPanel.enabled = false;
        }
    }

    public void ShowStageClear()
    {
        hasResult = true;
        ShowResult("STAGE CLEAR");
    }

    public void ShowStageFailed(string reason)
    {
        hasResult = true;
        ShowResult("STAGE FAILED\n" + reason);
    }

    public void ShowGameOver(string reason)
    {
        hasResult = true;
        ShowResult("GAME OVER\n" + reason);
    }

    public void SetCuteCooldown(float remainingSeconds, float totalSeconds)
    {
        if (cooldownText == null)
        {
            return;
        }

        float safeRemaining = Mathf.Max(0f, remainingSeconds);
        if (safeRemaining > 0f)
        {
            cooldownText.text = "Q Cute: " + safeRemaining.ToString("0.0") + "s";
        }
        else
        {
            cooldownText.text = "Q Cute: READY";
        }

        cooldownText.enabled = true;
    }

    public void HideCuteCooldown()
    {
        if (cooldownText == null)
        {
            return;
        }

        cooldownText.text = "Q Cute: READY";
        cooldownText.enabled = true;
    }

    public void SetInteractionTarget(string targetName, string targetType, bool actionReady)
    {
        if (interactionText == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(targetName))
        {
            ClearInteractionTarget();
            return;
        }

        string action = actionReady ? "LMB to Mischief" : "E to Select";
        interactionText.text = "Target: " + targetName + " [" + targetType + "]\n" + action;
        interactionText.enabled = true;
    }

    public void ClearInteractionTarget()
    {
        if (interactionText == null)
        {
            return;
        }

        interactionText.text = "Target: none";
        interactionText.enabled = true;
    }

    public void ShowInteractionNotice(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.enabled = true;
            feedbackTimer = Mathf.Max(0.1f, feedbackMessageDuration);
        }
    }

    public void ShowMischiefApplied(string targetId, float rageAmount)
    {
        string label = string.IsNullOrWhiteSpace(targetId) ? "Mischief" : targetId;
        ShowInteractionNotice(label + " mischief! Rage +" + rageAmount.ToString("0"));
    }

    public void ShowHideVisual(bool isHidden, float remainingSeconds)
    {
        hiddenVisualActive = isHidden;

        if (hideOverlay != null)
        {
            hideOverlay.enabled = isHidden;
        }

        if (hiddenText != null)
        {
            hiddenText.enabled = isHidden;
            hiddenText.text = isHidden
                ? "HIDDEN\nF: Exit box\nTime left: " + Mathf.Max(0f, remainingSeconds).ToString("0.0") + "s"
                : string.Empty;
        }
    }

    private void PollCoreState()
    {
        if (coreFacade == null)
        {
            return;
        }

        SetScore(coreFacade.CurrentScore, coreFacade.TargetScore);
        SetScoreMultiplier(coreFacade.CurrentMultiplier);

        if (!string.IsNullOrWhiteSpace(watchedNpcId))
        {
            lastRage = coreFacade.GetRage(watchedNpcId);
            lastRageState = coreFacade.GetRageState(watchedNpcId);
            UpdateRageText();
        }

        if (coreFacade.IsPlayerHidden)
        {
            SetTimer(coreFacade.RemainingHideTime);
            ShowHideVisual(true, coreFacade.RemainingHideTime);
        }
        else
        {
            if (timerText != null && !hasResult)
            {
                timerText.text = string.Empty;
            }

            if (hiddenVisualActive)
            {
                ShowHideVisual(false, 0f);
            }
        }

        if (coreFacade.StageCleared && !hasResult)
        {
            ShowStageClear();
        }
        else if (coreFacade.StageFailed && !hasResult)
        {
            ShowStageFailed("Caught before enough score");
        }
    }

    private void TickFeedbackMessage()
    {
        if (feedbackText == null || feedbackTimer <= 0f)
        {
            return;
        }

        feedbackTimer -= Time.deltaTime;
        if (feedbackTimer <= 0f)
        {
            feedbackText.enabled = false;
            feedbackText.text = string.Empty;
        }
    }

    private void RefreshAllText()
    {
        UpdateScoreText();
        UpdateMultiplierText();
        UpdateRageText();

        if (objectiveText != null)
        {
            objectiveText.text = string.IsNullOrEmpty(lastObjective) ? "Objective: Reach target score, then survive being caught." : lastObjective;
        }

        if (promptText != null)
        {
            promptText.text = lastPrompt;
            promptText.enabled = !string.IsNullOrEmpty(lastPrompt);
        }

        if (promptPanel != null)
        {
            promptPanel.enabled = !string.IsNullOrEmpty(lastPrompt);
        }

        if (dangerText != null)
        {
            dangerText.text = lastDanger;
            dangerText.enabled = !string.IsNullOrEmpty(lastDanger);
        }

        if (dangerPanel != null)
        {
            dangerPanel.enabled = !string.IsNullOrEmpty(lastDanger);
        }

        if (cooldownText != null)
        {
            cooldownText.text = "Q Cute: READY";
        }

        if (controlsText != null)
        {
            controlsText.text = "WASD Move  |  Mouse Camera  |  Space Jump  |  E Select  |  LMB Mischief  |  Q Cute  |  F Hide";
        }

        ClearInteractionTarget();
        ShowHideVisual(false, 0f);
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + lastScore + " / " + lastTargetScore;
        }

        if (scoreFill != null)
        {
            float ratio = lastTargetScore > 0 ? Mathf.Clamp01((float)lastScore / lastTargetScore) : 0f;
            scoreFill.fillAmount = ratio;
        }
    }

    private void UpdateMultiplierText()
    {
        if (multiplierText != null)
        {
            multiplierText.text = "Multiplier: x" + lastMultiplier.ToString("0.00");
        }

        if (multiplierFill != null)
        {
            multiplierFill.fillAmount = Mathf.Clamp01(lastMultiplier / 13f);
        }
    }

    private void UpdateRageText()
    {
        if (rageText != null)
        {
            rageText.text = watchedNpcId + " Rage: " + lastRage.ToString("0") + "% (" + lastRageState + ")";
        }

        if (rageFill != null)
        {
            rageFill.fillAmount = Mathf.Clamp01(lastRage / 100f);
            rageFill.color = GetRageColor(lastRage);
        }
    }

    private Color GetRageColor(float rage)
    {
        if (rage >= 100f)
        {
            return rageMaxColor;
        }

        if (rage >= 70f)
        {
            return rageAngryColor;
        }

        return rageCalmColor;
    }

    private void ShowResult(string message)
    {
        if (resultText != null)
        {
            resultText.text = message;
            resultText.enabled = true;
        }
    }

    private void EnsureUI()
    {
        if (canvas == null)
        {
            canvas = GetComponentInChildren<Canvas>();
        }

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("RuntimeCanvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        GameObject topLeft = CreatePanel("TopLeftStatusPanel", canvas.transform, new Vector2(24f, -24f), TextAnchor.UpperLeft, new Vector2(620f, 170f), panelColor);
        scoreText = scoreText != null ? scoreText : CreateText("ScoreText", new Vector2(16f, -12f), TextAnchor.UpperLeft, font, 28, topLeft.transform, new Vector2(560f, 36f));
        scoreFill = scoreFill != null ? scoreFill : CreateBar("ScoreBar", new Vector2(16f, -52f), topLeft.transform, scoreColor);
        multiplierText = multiplierText != null ? multiplierText : CreateText("MultiplierText", new Vector2(16f, -80f), TextAnchor.UpperLeft, font, 26, topLeft.transform, new Vector2(560f, 34f));
        multiplierFill = multiplierFill != null ? multiplierFill : CreateBar("MultiplierBar", new Vector2(16f, -118f), topLeft.transform, multiplierColor);
        objectiveText = objectiveText != null ? objectiveText : CreateText("ObjectiveText", new Vector2(16f, -146f), TextAnchor.UpperLeft, font, 20, topLeft.transform, new Vector2(580f, 42f));

        GameObject topRight = CreatePanel("TopRightRagePanel", canvas.transform, new Vector2(-24f, -24f), TextAnchor.UpperRight, new Vector2(580f, 130f), panelColor);
        rageText = rageText != null ? rageText : CreateText("RageText", new Vector2(-16f, -12f), TextAnchor.UpperRight, font, 28, topRight.transform, new Vector2(540f, 36f));
        rageFill = rageFill != null ? rageFill : CreateBar("RageBar", new Vector2(40f, -52f), topRight.transform, rageCalmColor);
        cooldownText = cooldownText != null ? cooldownText : CreateText("CooldownText", new Vector2(-16f, -84f), TextAnchor.UpperRight, font, 22, topRight.transform, new Vector2(540f, 34f));

        GameObject bottomLeft = CreatePanel("InteractionPanel", canvas.transform, new Vector2(24f, 24f), TextAnchor.LowerLeft, new Vector2(560f, 120f), panelColor);
        interactionText = interactionText != null ? interactionText : CreateText("InteractionText", new Vector2(16f, 84f), TextAnchor.UpperLeft, font, 26, bottomLeft.transform, new Vector2(520f, 76f));
        controlsText = controlsText != null ? controlsText : CreateText("ControlsText", new Vector2(16f, 16f), TextAnchor.LowerLeft, font, 18, bottomLeft.transform, new Vector2(520f, 36f));

        promptPanel = promptPanel != null ? promptPanel : CreatePanelImage("PromptPanel", canvas.transform, new Vector2(0f, 120f), TextAnchor.LowerCenter, new Vector2(760f, 74f), new Color(0f, 0f, 0f, 0.72f));
        promptText = promptText != null ? promptText : CreateText("PromptText", new Vector2(0f, 120f), TextAnchor.LowerCenter, font, 36, canvas.transform, new Vector2(740f, 70f));

        timerText = timerText != null ? timerText : CreateText("TimerText", new Vector2(0f, -116f), TextAnchor.UpperCenter, font, 32, canvas.transform, new Vector2(800f, 56f));
        feedbackText = feedbackText != null ? feedbackText : CreateText("FeedbackText", new Vector2(0f, -176f), TextAnchor.UpperCenter, font, 34, canvas.transform, new Vector2(900f, 64f));
        dangerPanel = dangerPanel != null ? dangerPanel : CreatePanelImage("DangerPanel", canvas.transform, new Vector2(0f, -238f), TextAnchor.UpperCenter, new Vector2(920f, 72f), new Color(0.35f, 0f, 0f, 0.7f));
        dangerText = dangerText != null ? dangerText : CreateText("DangerText", new Vector2(0f, -238f), TextAnchor.UpperCenter, font, 34, canvas.transform, new Vector2(900f, 68f));
        resultText = resultText != null ? resultText : CreateText("ResultText", Vector2.zero, TextAnchor.MiddleCenter, font, 64, canvas.transform, new Vector2(1100f, 220f));

        hideOverlay = hideOverlay != null ? hideOverlay : CreateFullScreenOverlay("HideOverlay", canvas.transform, new Color(0f, 0f, 0f, 0.52f));
        hiddenText = hiddenText != null ? hiddenText : CreateText("HiddenText", Vector2.zero, TextAnchor.MiddleCenter, font, 58, canvas.transform, new Vector2(1000f, 240f));

        if (resultText != null)
        {
            resultText.enabled = false;
        }

        if (feedbackText != null)
        {
            feedbackText.enabled = false;
        }
    }

    private GameObject CreatePanel(string objectName, Transform parent, Vector2 anchoredPosition, TextAnchor alignment, Vector2 size, Color color)
    {
        Image image = CreatePanelImage(objectName, parent, anchoredPosition, alignment, size, color);
        return image.gameObject;
    }

    private Image CreatePanelImage(string objectName, Transform parent, Vector2 anchoredPosition, TextAnchor alignment, Vector2 size, Color color)
    {
        GameObject panelObject = new GameObject(objectName);
        panelObject.transform.SetParent(parent, false);

        RectTransform rectTransform = panelObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = GetAnchor(alignment);
        rectTransform.anchorMax = GetAnchor(alignment);
        rectTransform.pivot = GetPivot(alignment);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Image image = panelObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private Image CreateBar(string objectName, Vector2 anchoredPosition, Transform parent, Color color)
    {
        GameObject background = new GameObject(objectName + "Background");
        background.transform.SetParent(parent, false);
        RectTransform backgroundRect = background.AddComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0f, 1f);
        backgroundRect.anchorMax = new Vector2(0f, 1f);
        backgroundRect.pivot = new Vector2(0f, 1f);
        backgroundRect.anchoredPosition = anchoredPosition;
        backgroundRect.sizeDelta = new Vector2(520f, 18f);
        Image backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(1f, 1f, 1f, 0.18f);

        GameObject fill = new GameObject(objectName + "Fill");
        fill.transform.SetParent(background.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = 0;
        fillImage.color = color;
        fillImage.fillAmount = 0f;
        return fillImage;
    }

    private Image CreateFullScreenOverlay(string objectName, Transform parent, Color color)
    {
        GameObject overlay = new GameObject(objectName);
        overlay.transform.SetParent(parent, false);
        overlay.transform.SetAsFirstSibling();

        RectTransform rectTransform = overlay.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = overlay.AddComponent<Image>();
        image.color = color;
        image.enabled = false;
        return image;
    }

    private Text CreateText(string objectName, Vector2 anchoredPosition, TextAnchor alignment, Font font, int fontSize, Transform parent, Vector2 size)
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.AddComponent<RectTransform>();
        rectTransform.anchorMin = GetAnchor(alignment);
        rectTransform.anchorMax = GetAnchor(alignment);
        rectTransform.pivot = GetPivot(alignment);
        rectTransform.anchoredPosition = anchoredPosition;
        rectTransform.sizeDelta = size;

        Text text = textObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static Vector2 GetAnchor(TextAnchor alignment)
    {
        switch (alignment)
        {
            case TextAnchor.UpperLeft:
                return new Vector2(0f, 1f);
            case TextAnchor.UpperRight:
                return new Vector2(1f, 1f);
            case TextAnchor.UpperCenter:
                return new Vector2(0.5f, 1f);
            case TextAnchor.LowerLeft:
                return new Vector2(0f, 0f);
            case TextAnchor.LowerCenter:
                return new Vector2(0.5f, 0f);
            case TextAnchor.MiddleCenter:
                return new Vector2(0.5f, 0.5f);
            default:
                return new Vector2(0.5f, 0.5f);
        }
    }

    private static Vector2 GetPivot(TextAnchor alignment)
    {
        switch (alignment)
        {
            case TextAnchor.UpperLeft:
                return new Vector2(0f, 1f);
            case TextAnchor.UpperRight:
                return new Vector2(1f, 1f);
            case TextAnchor.UpperCenter:
                return new Vector2(0.5f, 1f);
            case TextAnchor.LowerLeft:
                return new Vector2(0f, 0f);
            case TextAnchor.LowerCenter:
                return new Vector2(0.5f, 0f);
            case TextAnchor.MiddleCenter:
                return new Vector2(0.5f, 0.5f);
            default:
                return new Vector2(0.5f, 0.5f);
        }
    }
}
