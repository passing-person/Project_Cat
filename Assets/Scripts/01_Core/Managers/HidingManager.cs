using System.Collections.Generic;
using UnityEngine;

public class HidingManager : MonoBehaviour
{
    [Header("References")]
    public ScoreManager scoreManager;
    public MonoBehaviour uiBridgeBehaviour;

    [Header("Config")]
    public float maxHideDuration = 10f;
    public float hiddenMultiplierScale = 0.1f;
    public int hideSpotUsesPerStage = 1;
    public bool autoTick = true;

    [Header("Debug")]
    [SerializeField] private bool isHidden;
    [SerializeField] private string activeHideSpotId;
    [SerializeField] private float hiddenElapsedTime;
    [SerializeField] private bool forcedOutByTimer;

    private readonly Dictionary<string, int> hideSpotUseCounts = new Dictionary<string, int>();
    private ICoreUIBridge uiBridge;

    public bool IsHidden => isHidden;
    public string ActiveHideSpotId => activeHideSpotId;
    public float HiddenElapsedTime => hiddenElapsedTime;
    public float RemainingHideTime => Mathf.Max(0f, maxHideDuration - hiddenElapsedTime);
    public bool WasForcedOutByTimer => forcedOutByTimer;

    private void Awake()
    {
        ResolveUIBridge();
    }

    private void Update()
    {
        if (autoTick)
        {
            TickHiding(Time.deltaTime);
        }
    }

    public void Configure(float newMaxHideDuration, float newHiddenMultiplierScale, int newUsesPerStage)
    {
        maxHideDuration = Mathf.Max(0f, newMaxHideDuration);
        hiddenMultiplierScale = Mathf.Clamp01(newHiddenMultiplierScale);
        hideSpotUsesPerStage = Mathf.Max(1, newUsesPerStage);
    }

    public void ConfigureFromStageData(StageData stageData)
    {
        if (stageData == null)
        {
            return;
        }

        Configure(stageData.maxHideDuration, stageData.hiddenMultiplierScale, stageData.hideSpotUsesPerStage);
    }

    public bool CanUseHideSpot(string hideSpotId)
    {
        if (string.IsNullOrWhiteSpace(hideSpotId) || isHidden)
        {
            return false;
        }

        int usedCount = GetHideSpotUseCount(hideSpotId);
        return usedCount < hideSpotUsesPerStage;
    }

    public bool ReportPlayerHidden(string hideSpotId)
    {
        if (!CanUseHideSpot(hideSpotId))
        {
            return false;
        }

        isHidden = true;
        activeHideSpotId = hideSpotId;
        hiddenElapsedTime = 0f;
        forcedOutByTimer = false;
        hideSpotUseCounts[hideSpotId] = GetHideSpotUseCount(hideSpotId) + 1;

        if (scoreManager != null)
        {
            scoreManager.SetTemporaryMultiplierScale(hiddenMultiplierScale);
        }

        RefreshTimerUI();
        return true;
    }

    public void ReportPlayerExitHiding()
    {
        if (!isHidden)
        {
            return;
        }

        isHidden = false;
        activeHideSpotId = string.Empty;
        hiddenElapsedTime = 0f;

        if (scoreManager != null)
        {
            scoreManager.ClearTemporaryMultiplierScale();
        }

        RefreshTimerUI();
    }

    public void TickHiding(float deltaTime)
    {
        if (!isHidden || deltaTime <= 0f)
        {
            return;
        }

        hiddenElapsedTime += deltaTime;
        RefreshTimerUI();

        if (maxHideDuration > 0f && hiddenElapsedTime >= maxHideDuration)
        {
            forcedOutByTimer = true;
            ReportPlayerExitHiding();
        }
    }

    public bool HasUsedHideSpot(string hideSpotId)
    {
        return GetHideSpotUseCount(hideSpotId) > 0;
    }

    public int GetHideSpotUseCount(string hideSpotId)
    {
        if (string.IsNullOrWhiteSpace(hideSpotId))
        {
            return 0;
        }

        return hideSpotUseCounts.TryGetValue(hideSpotId, out int count) ? count : 0;
    }

    public void ResetHidingState()
    {
        hideSpotUseCounts.Clear();
        isHidden = false;
        activeHideSpotId = string.Empty;
        hiddenElapsedTime = 0f;
        forcedOutByTimer = false;

        if (scoreManager != null)
        {
            scoreManager.ClearTemporaryMultiplierScale();
        }

        RefreshTimerUI();
    }

    public void SetUIBridge(ICoreUIBridge bridge)
    {
        uiBridge = bridge;
        RefreshTimerUI();
    }

    private void ResolveUIBridge()
    {
        uiBridge = uiBridgeBehaviour as ICoreUIBridge;
    }

    private void RefreshTimerUI()
    {
        if (uiBridge == null)
        {
            ResolveUIBridge();
        }

        if (uiBridge != null)
        {
            uiBridge.SetTimer(RemainingHideTime);
        }
    }
}
