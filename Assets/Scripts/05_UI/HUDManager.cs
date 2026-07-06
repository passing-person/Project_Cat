using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text multiplierText;
    public TMP_Text targetScoreText;

    private int currentScore;
    private int currentTargetScore;
    private float currentMultiplier = 1f;

    private void Start()
    {
        RefreshUI();
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

    /// <summary>
    /// 更新分数和目标分数
    /// 给 UICoreBridge 调用
    /// </summary>
    public void SetScore(int score, int targetScore)
    {
        currentScore = score;
        currentTargetScore = targetScore;

        RefreshUI();
    }

    /// <summary>
    /// 更新倍率
    /// 给 UICoreBridge 调用
    /// </summary>
    public void SetMultiplier(float multiplier)
    {
        currentMultiplier = multiplier;

        RefreshUI();
    }

    /// <summary>
    /// 单独更新目标分数
    /// </summary>
    public void SetTargetScore(int targetScore)
    {
        currentTargetScore = targetScore;

        RefreshUI();
    }
}