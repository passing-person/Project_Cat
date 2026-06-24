using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [Header("References")]
    public MonoBehaviour uiBridgeBehaviour;

    [Header("Config")]
    public int targetScore = 300;
    public float baseScoreRate = 10f;
    public float maxScoreMultiplierBonus = 12f;
    public bool autoTick = true;

    [Header("Debug")]
    [SerializeField] private float currentScore;
    [SerializeField] private float currentMultiplier = 1f;
    [SerializeField] private float temporaryMultiplierScale = 1f;
    [SerializeField] private bool isScoring;
    [SerializeField] private bool multiplierOverrideEnabled;
    [SerializeField] private float multiplierOverrideValue = 1f;
    [SerializeField] private float lastAverageRage;

    private ICoreUIBridge uiBridge;

    public int CurrentScore => Mathf.FloorToInt(currentScore);
    public float CurrentScoreFloat => currentScore;
    public float CurrentBaseMultiplier => currentMultiplier;
    public float CurrentMultiplier => currentMultiplier * temporaryMultiplierScale;
    public float TemporaryMultiplierScale => temporaryMultiplierScale;
    public bool HasTemporaryMultiplierScale => !Mathf.Approximately(temporaryMultiplierScale, 1f);
    public bool IsScoring => isScoring;
    public bool HasMultiplierOverride => multiplierOverrideEnabled;
    public float LastAverageRage => lastAverageRage;

    private void Awake()
    {
        ResolveUIBridge();
    }

    private void Update()
    {
        if (autoTick)
        {
            TickScore(Time.deltaTime);
        }
    }

    public void InitializeScore(int newTargetScore)
    {
        targetScore = Mathf.Max(0, newTargetScore);
        currentScore = 0f;
        currentMultiplier = 1f;
        temporaryMultiplierScale = 1f;
        lastAverageRage = 0f;
        isScoring = false;
        multiplierOverrideEnabled = false;
        multiplierOverrideValue = 1f;
        RefreshUI();
    }

    public void InitializeScore(int newTargetScore, float newBaseScoreRate, float newMaxBonus)
    {
        SetScoreConfig(newBaseScoreRate, newMaxBonus);
        InitializeScore(newTargetScore);
    }

    public void SetScoreConfig(float newBaseScoreRate, float newMaxBonus)
    {
        baseScoreRate = Mathf.Max(0f, newBaseScoreRate);
        maxScoreMultiplierBonus = Mathf.Max(0f, newMaxBonus);
    }

    public void StartScoring()
    {
        isScoring = true;
    }

    public void StopScoring()
    {
        isScoring = false;
    }

    public void TickScore(float deltaTime)
    {
        if (!isScoring || deltaTime <= 0f)
        {
            return;
        }

        currentScore += baseScoreRate * CurrentMultiplier * deltaTime;
        RefreshUI();
    }

    public void RecalculateMultiplier(float averageRage)
    {
        lastAverageRage = Mathf.Max(0f, averageRage);

        if (multiplierOverrideEnabled)
        {
            currentMultiplier = multiplierOverrideValue;
            RefreshMultiplierUI();
            return;
        }

        currentMultiplier = CalculateMultiplierFromAverageRage(lastAverageRage);
        RefreshMultiplierUI();
    }

    public float CalculateMultiplierFromAverageRage(float averageRage)
    {
        float normalizedRage = Mathf.Clamp01(averageRage / 100f);
        float rageFactor = normalizedRage * normalizedRage;
        return 1f + rageFactor * maxScoreMultiplierBonus;
    }

    public void SetMultiplierOverride(float multiplier, bool enabled)
    {
        multiplierOverrideEnabled = enabled;
        multiplierOverrideValue = Mathf.Max(0f, multiplier);

        if (enabled)
        {
            currentMultiplier = multiplierOverrideValue;
        }
        else
        {
            currentMultiplier = CalculateMultiplierFromAverageRage(lastAverageRage);
        }

        RefreshMultiplierUI();
    }

    public void SetTemporaryMultiplierScale(float scale)
    {
        temporaryMultiplierScale = Mathf.Clamp01(scale);
        RefreshMultiplierUI();
    }

    public void ClearTemporaryMultiplierScale()
    {
        SetTemporaryMultiplierScale(1f);
    }

    public int AddScore(int amount)
    {
        if (amount <= 0)
        {
            return CurrentScore;
        }

        currentScore += amount;
        RefreshUI();
        return CurrentScore;
    }

    public void SetScore(float value)
    {
        currentScore = Mathf.Max(0f, value);
        RefreshUI();
    }

    public bool HasReachedTargetScore()
    {
        return CurrentScore >= targetScore;
    }

    public void ResetScore()
    {
        currentScore = 0f;
        currentMultiplier = 1f;
        temporaryMultiplierScale = 1f;
        lastAverageRage = 0f;
        isScoring = false;
        multiplierOverrideEnabled = false;
        multiplierOverrideValue = 1f;
        RefreshUI();
    }

    public void SetUIBridge(ICoreUIBridge bridge)
    {
        uiBridge = bridge;
        RefreshUI();
    }

    public bool IsValid(out string message)
    {
        if (targetScore < 0)
        {
            message = "ScoreManager.targetScore cannot be negative.";
            return false;
        }

        if (baseScoreRate < 0f)
        {
            message = "ScoreManager.baseScoreRate cannot be negative.";
            return false;
        }

        if (maxScoreMultiplierBonus < 0f)
        {
            message = "ScoreManager.maxScoreMultiplierBonus cannot be negative.";
            return false;
        }

        if (temporaryMultiplierScale < 0f || temporaryMultiplierScale > 1f)
        {
            message = "ScoreManager.temporaryMultiplierScale must be between 0 and 1.";
            return false;
        }

        message = "ScoreManager is valid.";
        return true;
    }

    private void ResolveUIBridge()
    {
        uiBridge = uiBridgeBehaviour as ICoreUIBridge;
    }

    private void RefreshUI()
    {
        if (uiBridge == null)
        {
            ResolveUIBridge();
        }

        if (uiBridge == null)
        {
            return;
        }

        uiBridge.SetScore(CurrentScore, targetScore);
        uiBridge.SetScoreMultiplier(CurrentMultiplier);
    }

    private void RefreshMultiplierUI()
    {
        if (uiBridge == null)
        {
            ResolveUIBridge();
        }

        if (uiBridge != null)
        {
            uiBridge.SetScoreMultiplier(CurrentMultiplier);
        }
    }
}
