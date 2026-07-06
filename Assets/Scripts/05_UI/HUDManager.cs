using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text multiplierText;
    public TMP_Text targetScoreText;

    [Header("Core Binding")]
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private bool autoFindCore = true;
    [SerializeField] private bool pollCore = true;
    [SerializeField] private float pollInterval = 0.05f;

    private int currentScore;
    private int currentTargetScore = 5000;
    private float currentMultiplier = 1f;
    private float pollTimer;

    private void Start()
    {
        ResolveCore();
        RefreshFromCore();
        RefreshUI();
    }

    private void Update()
    {
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
        ResolveCore();
        RefreshFromCore();
    }

    public void BindCore(CoreFacade facade)
    {
        coreFacade = facade;
        RefreshFromCore();
    }

    private void ResolveCore()
    {
        if (!autoFindCore || coreFacade != null)
        {
            return;
        }

        coreFacade = FindObjectOfType<CoreFacade>();
    }

    private void RefreshFromCore()
    {
        if (coreFacade == null)
        {
            RefreshUI();
            return;
        }

        SetScore(coreFacade.CurrentScore, coreFacade.TargetScore);
        SetMultiplier(coreFacade.CurrentMultiplier);
    }

    private void RefreshUI()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }

        if (multiplierText != null)
        {
            multiplierText.text = currentMultiplier.ToString("F1") + "x";
        }

        if (targetScoreText != null)
        {
            targetScoreText.text = "目标：" + currentTargetScore;
        }
    }

    public void SetScore(int score, int targetScore)
    {
        currentScore = Mathf.Max(0, score);
        currentTargetScore = targetScore > 0 ? targetScore : currentTargetScore;
        RefreshUI();
    }

    public void SetMultiplier(float multiplier)
    {
        currentMultiplier = Mathf.Max(0f, multiplier);
        RefreshUI();
    }

    public void SetTargetScore(int targetScore)
    {
        currentTargetScore = Mathf.Max(0, targetScore);
        RefreshUI();
    }
}
