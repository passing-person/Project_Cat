using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    [Header("UI")]
    public Image cooldownMask;
    public TMP_Text cooldownText;

    [Header("Cooldown")]
    public float cooldownTime = 20f;

    private float timer = 0f;
    private bool isCooling = false;

    void Start()
    {
        cooldownMask.fillAmount = 0;
        cooldownText.text = "";
    }

    void Update()
    {
        // 按Q开始冷却（以后这里可以改成真正释放技能后调用）
        if (Input.GetKeyDown(KeyCode.Q) && !isCooling)
        {
            StartCooldown();
        }

        if (isCooling)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                FinishCooldown();
            }
            else
            {
                UpdateCooldownUI();
            }
        }
    }

    void StartCooldown()
    {
        isCooling = true;
        timer = cooldownTime;

        cooldownMask.enabled = true;
        cooldownText.enabled = true;
    }

    void FinishCooldown()
    {
        isCooling = false;

        cooldownMask.fillAmount = 0;

        cooldownMask.enabled = false;

        cooldownText.text = "";

        cooldownText.enabled = false;
    }

    void UpdateCooldownUI()
    {
        // 显示整数秒
        cooldownText.text = Mathf.Ceil(timer).ToString();

        // FillAmount
        cooldownMask.fillAmount = timer / cooldownTime;
    }
}