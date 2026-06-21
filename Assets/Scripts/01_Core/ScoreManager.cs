using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public int CurrentScore => Mathf.FloorToInt(currentScore);
    public int TargetScore { get; private set; }
    public float ScoreMultiplier => multiplierOverrideEnabled ? multiplierOverride : scoreMultiplier;
    public bool IsScoring { get; private set; }

    [Header("UI")]
    [SerializeField] private UIManager uiManager;

    private float currentScore;
    private float baseScoreRate = 10f;
    private float maxBonus = 12f;
    private float scoreMultiplier = 1f;
    private float multiplierOverride = 1f;
    private bool multiplierOverrideEnabled;

    public void InitializeScore(int targetScore)
    {
        TargetScore = targetScore;
        ResetScore();
    }

    public void SetScoreConfig(float baseRate, float maxMultiplierBonus)
    {
        baseScoreRate = Mathf.Max(0f, baseRate);
        maxBonus = Mathf.Max(0f, maxMultiplierBonus);
    }

    public void StartScoring()
    {
        IsScoring = true;
    }

    public void StopScoring()
    {
        IsScoring = false;
    }

    private void Update()
    {
        TickScore(Time.deltaTime);
    }

    public void TickScore(float deltaTime)
    {
        if (!IsScoring)
            return;

        currentScore += baseScoreRate * ScoreMultiplier * deltaTime;
        RefreshUI();
    }

    public void RecalculateMultiplier(float averageRage)
    {
        float normalizedRage = Mathf.Clamp01(averageRage / 100f);
        float rageFactor = normalizedRage * normalizedRage;
        scoreMultiplier = 1f + rageFactor * maxBonus;

        RefreshUI();
    }

    public void SetMultiplierOverride(float multiplier, bool enabled)
    {
        multiplierOverride = Mathf.Max(1f, multiplier);
        multiplierOverrideEnabled = enabled;

        RefreshUI();
    }

    public int AddScore(int amount)
    {
        float previous = currentScore;
        currentScore += Mathf.Max(0, amount);
        RefreshUI();
        return Mathf.FloorToInt(currentScore - previous);
    }

    public bool HasReachedTargetScore()
    {
        return CurrentScore >= TargetScore;
    }

    public void ResetScore()
    {
        currentScore = 0f;
        scoreMultiplier = 1f;
        multiplierOverride = 1f;
        multiplierOverrideEnabled = false;
        IsScoring = false;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (uiManager == null)
            return;

        uiManager.SetScore(CurrentScore, TargetScore);
        uiManager.SetScoreMultiplier(ScoreMultiplier);
    }
}
