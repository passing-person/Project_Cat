using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour, ICoreUIBridge
{
    [Header("References")]
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private SimpleFeedbackAudio feedbackAudio;
    [SerializeField] private ThirdPersonCameraController cameraController;

    [Header("Runtime UI")]
    [SerializeField] private bool autoFindReferences = true;
    [SerializeField] private bool useImmediateModeOverlay = true;
    [SerializeField] private string watchedNpcId = "Supervisor";
    [SerializeField] private float pollInterval = 0.05f;
    [SerializeField] private float feedbackMessageDuration = 1.35f;
    [SerializeField] private bool showWorldRageBars = true;
    [SerializeField] private Vector3 worldRageBarOffset = new Vector3(0f, 2.1f, 0f);

    private float pollTimer;
    private float feedbackTimer;
    private float screenFlashTimer;
    private float screenFlashDuration = 0.25f;
    private Color screenFlashColor = Color.clear;

    private int lastScore;
    private int lastTargetScore = 300;
    private float lastMultiplier = 1f;
    private float lastRage;
    private NpcRageState lastRageState = NpcRageState.Calm;
    private string lastObjective = "Reach target score, then survive being caught.";
    private string lastPrompt = string.Empty;
    private string lastDanger = string.Empty;
    private string lastInteractionTarget = "Target: none";
    private string feedbackTitle = string.Empty;
    private string feedbackDetail = string.Empty;
    private string resultMessage = string.Empty;
    private bool hasResult;
    private bool isHidden;
    private float hideRemaining;
    private float cuteCooldownRemaining;
    private float cuteCooldownTotal = 20f;

    private GUIStyle panelStyle;
    private GUIStyle headerStyle;
    private GUIStyle textStyle;
    private GUIStyle smallTextStyle;
    private GUIStyle centerPromptStyle;
    private GUIStyle resultStyle;
    private GUIStyle feedbackStyle;
    private Texture2D whiteTexture;
    private Texture2D panelTexture;
    private readonly Queue<string> eventLog = new Queue<string>();
    private const int MaxLogLines = 4;

    private void Awake()
    {
        EnsureReferences();
    }

    private void Start()
    {
        EnsureReferences();
        if (coreFacade != null)
        {
            coreFacade.SetUIBridge(this);
        }
    }

    private void Update()
    {
        if (autoFindReferences)
        {
            EnsureReferences();
        }

        TickFeedback();

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
        if (!string.IsNullOrEmpty(npcId))
        {
            watchedNpcId = npcId;
        }
    }

    public void SetScore(int currentScore, int targetScore)
    {
        lastScore = Mathf.Max(0, currentScore);
        lastTargetScore = Mathf.Max(0, targetScore);
    }

    public void SetScoreMultiplier(float multiplier)
    {
        lastMultiplier = Mathf.Max(0f, multiplier);
    }

    public void SetRage(string npcId, float currentRage, NpcRageState state)
    {
        if (!string.IsNullOrEmpty(npcId))
        {
            watchedNpcId = npcId;
        }

        float previousRage = lastRage;
        lastRage = Mathf.Clamp(currentRage, 0f, 100f);
        lastRageState = state;

        if (lastRage > previousRage + 0.1f)
        {
            ShowActionFeedback("RAGE UP", watchedNpcId + " +" + (lastRage - previousRage).ToString("0"), new Color(1f, 0.35f, 0.1f, 0.32f));
        }
    }

    public void SetTimer(float time)
    {
        hideRemaining = Mathf.Max(0f, time);
    }

    public void SetObjectiveText(string text)
    {
        lastObjective = string.IsNullOrEmpty(text) ? string.Empty : text;
    }

    public void ShowPrompt(string message)
    {
        lastPrompt = string.IsNullOrEmpty(message) ? string.Empty : message;
    }

    public void HidePrompt()
    {
        lastPrompt = string.Empty;
    }

    public void ShowDangerWarning(string message)
    {
        lastDanger = string.IsNullOrEmpty(message) ? string.Empty : message;
        if (!string.IsNullOrEmpty(lastDanger))
        {
            Flash(new Color(1f, 0f, 0f, 0.25f), 0.35f);
            feedbackAudio?.PlayError();
        }
    }

    public void HideDangerWarning()
    {
        lastDanger = string.Empty;
    }

    public void ShowStageClear()
    {
        hasResult = true;
        resultMessage = "STAGE CLEAR";
        ShowActionFeedback("CLEAR", "Target score reached", new Color(0.2f, 1f, 0.4f, 0.28f));
        feedbackAudio?.PlayClear();
    }

    public void ShowStageFailed(string reason)
    {
        hasResult = true;
        resultMessage = "STAGE FAILED\n" + reason;
        ShowActionFeedback("FAILED", reason, new Color(1f, 0f, 0f, 0.32f));
        feedbackAudio?.PlayFail();
    }

    public void ShowGameOver(string reason)
    {
        hasResult = true;
        resultMessage = "GAME OVER\n" + reason;
        ShowActionFeedback("GAME OVER", reason, new Color(1f, 0f, 0f, 0.32f));
        feedbackAudio?.PlayFail();
    }

    public void SetCuteCooldown(float remainingSeconds, float totalSeconds)
    {
        cuteCooldownRemaining = Mathf.Max(0f, remainingSeconds);
        cuteCooldownTotal = Mathf.Max(0.1f, totalSeconds);
    }

    public void HideCuteCooldown()
    {
        cuteCooldownRemaining = 0f;
    }

    public void SetInteractionTarget(string targetName, string targetType, bool actionReady)
    {
        if (string.IsNullOrEmpty(targetName))
        {
            ClearInteractionTarget();
            return;
        }

        string action = actionReady ? "LMB" : "E";
        string actionText = actionReady ? "Mischief" : "Select";
        lastInteractionTarget = targetName + "  |  " + targetType + "  |  [" + action + "] " + actionText;
    }

    public void ClearInteractionTarget()
    {
        lastInteractionTarget = "Target: none";
    }

    public void ShowInteractionNotice(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        ShowActionFeedback("ACTION", message, new Color(0.2f, 0.65f, 1f, 0.22f));
        feedbackAudio?.PlaySelect();
    }

    public void ShowMischiefApplied(string targetId, float rageAmount)
    {
        string targetLabel = string.IsNullOrEmpty(targetId) ? "Target" : targetId;
        ShowActionFeedback("MISCHIEF!", targetLabel + "  |  Rage +" + rageAmount.ToString("0"), new Color(1f, 0.55f, 0f, 0.32f));
        feedbackAudio?.PlayMischief();
        cameraController?.AddImpulse(0.13f, 0.22f);
    }

    public void ShowCuteApplied(List<RageResult> results, float reductionAmount)
    {
        if (results == null || results.Count == 0)
        {
            ShowActionFeedback("CUTE FAILED", "No NPC in range", new Color(0.4f, 0.4f, 0.4f, 0.25f));
            feedbackAudio?.PlayError();
            return;
        }

        string targetLabel = results.Count == 1 ? results[0].NpcId : results.Count + " NPCs";
        bool stoppedChase = false;
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].PreviousRage >= 100f && results[i].CurrentRage < 100f)
            {
                stoppedChase = true;
                break;
            }
        }

        string detail = targetLabel + "  |  Rage -" + reductionAmount.ToString("0");
        if (stoppedChase)
        {
            detail += "  |  Chase stopped";
        }

        ShowActionFeedback("CUTE!", detail, new Color(1f, 0.35f, 0.85f, 0.32f));
        feedbackAudio?.PlayCute();
        cameraController?.AddImpulse(0.08f, 0.18f);
    }

    public void ShowActionBlocked(string reason)
    {
        ShowActionFeedback("BLOCKED", string.IsNullOrEmpty(reason) ? "Action unavailable" : reason, new Color(0.75f, 0.1f, 0.1f, 0.26f));
        feedbackAudio?.PlayError();
    }

    public void ShowHideVisual(bool hidden, float remainingSeconds)
    {
        bool changed = isHidden != hidden;
        isHidden = hidden;
        hideRemaining = Mathf.Max(0f, remainingSeconds);

        if (changed && hidden)
        {
            ShowActionFeedback("HIDDEN", "Box view. F to exit.", new Color(0f, 0f, 0f, 0.38f));
            feedbackAudio?.PlayHideEnter();
        }
        else if (changed)
        {
            ShowActionFeedback("EXITED", "Back to normal view", new Color(0.25f, 0.65f, 1f, 0.22f));
            feedbackAudio?.PlayHideExit();
        }
    }

    public void ShowActionFeedback(string title, string detail, Color flashColor)
    {
        feedbackTitle = string.IsNullOrEmpty(title) ? "ACTION" : title;
        feedbackDetail = string.IsNullOrEmpty(detail) ? string.Empty : detail;
        feedbackTimer = Mathf.Max(0.1f, feedbackMessageDuration);
        AddLog(feedbackTitle + (string.IsNullOrEmpty(feedbackDetail) ? string.Empty : " - " + feedbackDetail));
        Flash(flashColor, 0.25f);
    }

    private void PollCoreState()
    {
        if (coreFacade == null)
        {
            return;
        }

        lastScore = coreFacade.CurrentScore;
        lastTargetScore = coreFacade.TargetScore;
        lastMultiplier = coreFacade.CurrentMultiplier;

        if (!string.IsNullOrEmpty(watchedNpcId))
        {
            lastRage = coreFacade.GetRage(watchedNpcId);
            lastRageState = coreFacade.GetRageState(watchedNpcId);
        }

        ShowHideVisual(coreFacade.IsPlayerHidden, coreFacade.RemainingHideTime);

        if (coreFacade.StageCleared && !hasResult)
        {
            ShowStageClear();
        }
        else if (coreFacade.StageFailed && !hasResult)
        {
            ShowStageFailed("Caught before enough score");
        }
    }

    private void TickFeedback()
    {
        if (feedbackTimer > 0f)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0f)
            {
                feedbackTitle = string.Empty;
                feedbackDetail = string.Empty;
            }
        }

        if (screenFlashTimer > 0f)
        {
            screenFlashTimer -= Time.deltaTime;
        }
    }

    private void Flash(Color color, float duration)
    {
        screenFlashColor = color;
        screenFlashDuration = Mathf.Max(0.01f, duration);
        screenFlashTimer = screenFlashDuration;
    }

    private void AddLog(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return;
        }

        eventLog.Enqueue(line);
        while (eventLog.Count > MaxLogLines)
        {
            eventLog.Dequeue();
        }
    }

    private void EnsureReferences()
    {
        if (coreFacade == null) coreFacade = FindObjectOfType<CoreFacade>();
        if (feedbackAudio == null) feedbackAudio = FindObjectOfType<SimpleFeedbackAudio>();
        if (cameraController == null) cameraController = FindObjectOfType<ThirdPersonCameraController>();

        if (coreFacade != null && coreFacade.uiBridgeBehaviour != this)
        {
            coreFacade.SetUIBridge(this);
        }
    }

    private void OnGUI()
    {
        if (!useImmediateModeOverlay)
        {
            return;
        }

        EnsureGuiResources();
        DrawHiddenOverlay();
        DrawFlashOverlay();
        DrawTopLeftStatus();
        DrawTopRightRage();
        DrawWorldRageBars();
        DrawBottomControls();
        DrawPromptAndFeedback();
        DrawEventLog();
        DrawResult();
    }

    private void DrawTopLeftStatus()
    {
        Rect panel = new Rect(18f, 18f, 430f, 152f);
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(34f, 30f, 390f, 28f), "MISCHIEF SCORE", headerStyle);
        GUI.Label(new Rect(34f, 58f, 390f, 26f), lastScore + " / " + lastTargetScore, textStyle);
        DrawBar(new Rect(34f, 88f, 380f, 20f), GetScoreRatio(), new Color(0.1f, 0.9f, 0.35f, 1f));
        GUI.Label(new Rect(34f, 114f, 390f, 24f), "Multiplier  x" + lastMultiplier.ToString("0.00"), smallTextStyle);
        DrawBar(new Rect(34f, 140f, 380f, 14f), Mathf.Clamp01(lastMultiplier / 13f), new Color(0.2f, 0.55f, 1f, 1f));
    }

    private void DrawTopRightRage()
    {
        float width = 420f;
        Rect panel = new Rect(Screen.width - width - 18f, 18f, width, 132f);
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x + 16f, panel.y + 12f, width - 32f, 30f), watchedNpcId + " RAGE", headerStyle);
        GUI.Label(new Rect(panel.x + 16f, panel.y + 44f, width - 32f, 26f), lastRage.ToString("0") + "%  /  " + lastRageState, textStyle);
        DrawBar(new Rect(panel.x + 16f, panel.y + 78f, width - 32f, 24f), Mathf.Clamp01(lastRage / 100f), GetRageColor(lastRage));
        string cuteText = cuteCooldownRemaining > 0f ? "Q Cute cooldown: " + cuteCooldownRemaining.ToString("0.0") + "s" : "Q Cute: READY";
        GUI.Label(new Rect(panel.x + 16f, panel.y + 104f, width - 32f, 24f), cuteText, smallTextStyle);
    }

    private void DrawWorldRageBars()
    {
        if (!showWorldRageBars || coreFacade == null)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        List<string> npcIds = coreFacade.GetRegisteredNpcIds();
        for (int i = 0; i < npcIds.Count; i++)
        {
            string npcId = npcIds[i];
            if (string.IsNullOrEmpty(npcId))
            {
                continue;
            }

            if (!coreFacade.TryGetNpcWorldPosition(npcId, out Vector3 worldPosition))
            {
                continue;
            }

            Vector3 screenPosition = camera.WorldToScreenPoint(worldPosition + worldRageBarOffset);
            if (screenPosition.z <= 0f)
            {
                continue;
            }

            float rage = coreFacade.GetRage(npcId);
            float width = 160f;
            float height = 46f;
            Rect panel = new Rect(screenPosition.x - width * 0.5f, Screen.height - screenPosition.y - height * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panel.x + 8f, panel.y + 4f, width - 16f, 18f), npcId + " " + rage.ToString("0") + "%", smallTextStyle);
            DrawBar(new Rect(panel.x + 8f, panel.y + 26f, width - 16f, 12f), rage / 100f, GetRageColor(rage));
        }
    }

    private void DrawBottomControls()
    {
        Rect panel = new Rect(18f, Screen.height - 118f, Screen.width - 36f, 100f);
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x + 18f, panel.y + 12f, panel.width - 36f, 28f), lastInteractionTarget, textStyle);
        GUI.Label(new Rect(panel.x + 18f, panel.y + 46f, panel.width - 36f, 22f), "WASD Move  |  Shift Sprint  |  Mouse Camera  |  Space Jump", smallTextStyle);
        GUI.Label(new Rect(panel.x + 18f, panel.y + 70f, panel.width - 36f, 22f), "E Select / Interact  |  LMB Mischief  |  Q Cute  |  F Hide / Exit  |  Esc Cursor", smallTextStyle);
    }

    private void DrawPromptAndFeedback()
    {
        if (!string.IsNullOrEmpty(lastPrompt))
        {
            Rect promptRect = new Rect(Screen.width * 0.5f - 260f, Screen.height - 210f, 520f, 58f);
            GUI.Box(promptRect, GUIContent.none, panelStyle);
            GUI.Label(promptRect, lastPrompt, centerPromptStyle);
        }

        if (!string.IsNullOrEmpty(lastDanger))
        {
            Rect dangerRect = new Rect(Screen.width * 0.5f - 330f, 172f, 660f, 50f);
            GUI.Box(dangerRect, GUIContent.none, panelStyle);
            GUI.Label(dangerRect, lastDanger, centerPromptStyle);
        }

        if (feedbackTimer > 0f && !string.IsNullOrEmpty(feedbackTitle))
        {
            Rect feedbackRect = new Rect(Screen.width * 0.5f - 310f, Screen.height * 0.5f - 88f, 620f, 112f);
            GUI.Box(feedbackRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(feedbackRect.x, feedbackRect.y + 10f, feedbackRect.width, 44f), feedbackTitle, feedbackStyle);
            GUI.Label(new Rect(feedbackRect.x, feedbackRect.y + 62f, feedbackRect.width, 34f), feedbackDetail, centerPromptStyle);
        }
    }

    private void DrawHiddenOverlay()
    {
        if (!isHidden)
        {
            return;
        }

        GUI.color = new Color(0f, 0f, 0f, 0.42f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture);
        GUI.color = Color.white;

        Rect box = new Rect(Screen.width * 0.5f - 240f, 74f, 480f, 92f);
        GUI.Box(box, GUIContent.none, panelStyle);
        GUI.Label(new Rect(box.x, box.y + 10f, box.width, 38f), "HIDDEN", resultStyle);
        GUI.Label(new Rect(box.x, box.y + 54f, box.width, 28f), "Box view  |  F to exit  |  " + hideRemaining.ToString("0.0") + "s", centerPromptStyle);

        float vignette = 58f;
        GUI.color = new Color(0f, 0f, 0f, 0.58f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, vignette), whiteTexture);
        GUI.DrawTexture(new Rect(0f, Screen.height - vignette, Screen.width, vignette), whiteTexture);
        GUI.DrawTexture(new Rect(0f, 0f, vignette, Screen.height), whiteTexture);
        GUI.DrawTexture(new Rect(Screen.width - vignette, 0f, vignette, Screen.height), whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawFlashOverlay()
    {
        if (screenFlashTimer <= 0f)
        {
            return;
        }

        float alpha = Mathf.Clamp01(screenFlashTimer / screenFlashDuration) * screenFlashColor.a;
        GUI.color = new Color(screenFlashColor.r, screenFlashColor.g, screenFlashColor.b, alpha);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), whiteTexture);
        GUI.color = Color.white;
    }

    private void DrawEventLog()
    {
        if (eventLog.Count == 0)
        {
            return;
        }

        Rect panel = new Rect(18f, 186f, 480f, 30f + 24f * eventLog.Count);
        GUI.Box(panel, GUIContent.none, panelStyle);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 8f, panel.width - 28f, 22f), "EVENTS", smallTextStyle);
        int index = 0;
        foreach (string line in eventLog)
        {
            GUI.Label(new Rect(panel.x + 14f, panel.y + 32f + 22f * index, panel.width - 28f, 22f), line, smallTextStyle);
            index++;
        }
    }

    private void DrawResult()
    {
        if (!hasResult || string.IsNullOrEmpty(resultMessage))
        {
            return;
        }

        Rect resultRect = new Rect(Screen.width * 0.5f - 360f, Screen.height * 0.5f - 180f, 720f, 140f);
        GUI.Box(resultRect, GUIContent.none, panelStyle);
        GUI.Label(resultRect, resultMessage, resultStyle);
    }

    private void DrawBar(Rect rect, float ratio, Color fillColor)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = fillColor;
        GUI.DrawTexture(new Rect(rect.x + 2f, rect.y + 2f, Mathf.Max(0f, rect.width - 4f) * Mathf.Clamp01(ratio), rect.height - 4f), whiteTexture);
        GUI.color = Color.white;
    }

    private float GetScoreRatio()
    {
        return lastTargetScore > 0 ? Mathf.Clamp01((float)lastScore / lastTargetScore) : 0f;
    }

    private Color GetRageColor(float rage)
    {
        if (rage >= 100f) return new Color(1f, 0.05f, 0.02f, 1f);
        if (rage >= 70f) return new Color(1f, 0.4f, 0f, 1f);
        if (rage >= 40f) return new Color(1f, 0.85f, 0.1f, 1f);
        return new Color(0.25f, 0.9f, 0.35f, 1f);
    }

    private void EnsureGuiResources()
    {
        if (whiteTexture == null)
        {
            whiteTexture = Texture2D.whiteTexture;
        }

        if (panelTexture == null)
        {
            panelTexture = MakeTexture(new Color(0f, 0f, 0f, 0.72f));
        }

        if (panelStyle != null)
        {
            return;
        }

        panelStyle = new GUIStyle(GUI.skin.box);
        panelStyle.normal.background = panelTexture;
        panelStyle.normal.textColor = Color.white;

        headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
        smallTextStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = Color.white } };
        centerPromptStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        feedbackStyle = new GUIStyle(GUI.skin.label) { fontSize = 42, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
        resultStyle = new GUIStyle(GUI.skin.label) { fontSize = 46, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
    }
    private static Texture2D MakeTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

}

