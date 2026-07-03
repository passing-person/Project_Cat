using TMPro;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text scoreText;
    public TMP_Text multiplierText;
    public TMP_Text targetScoreText;

    [Header("Game Data")]
    public int score = 0;
    public float multiplier = 1.0f;
    public int targetScore = 300;

    void Start()
    {
        UpdateHUD();
    }

    public void UpdateHUD()
    {
        scoreText.text = "" + score;

        multiplierText.text = "" + multiplier.ToString("F1") + "x";

        targetScoreText.text = "Ŀ�꣺" + targetScore;
    }

    public void AddScore(int amount)
    {
        score += amount;

        UpdateHUD();
    }

    public void SetMultiplier(float value)
    {
        multiplier = value;

        UpdateHUD();
    }

    public void SetTargetScore(int value)
    {
        targetScore = value;

        UpdateHUD();
    }
}