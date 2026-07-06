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

    [Header("Core Binding")]
    [SerializeField] private PlayerCuteAction playerCuteAction;
    [SerializeField] private bool autoFindPlayerCuteAction = true;
    [SerializeField] private bool trackPlayerCuteAction = true;

    private float timer;
    private bool isCooling;

    private void Start()
    {
        ResolvePlayerCuteAction();
        FinishCooldown();
    }

    private void Update()
    {
        ResolvePlayerCuteAction();

        if (trackPlayerCuteAction && playerCuteAction != null)
        {
            SetCooldown(playerCuteAction.CooldownRemaining, playerCuteAction.CooldownDuration);
            return;
        }

        if (isCooling)
        {
            timer -= Time.unscaledDeltaTime;

            if (timer <= 0f)
            {
                FinishCooldown();
            }
            else
            {
                UpdateCooldownUI();
            }
        }
    }

    public void BindPlayerCuteAction(PlayerCuteAction cuteAction)
    {
        playerCuteAction = cuteAction;
        if (playerCuteAction != null)
        {
            SetCooldown(playerCuteAction.CooldownRemaining, playerCuteAction.CooldownDuration);
        }
    }

    public void SetCooldown(float remainingSeconds, float totalSeconds)
    {
        cooldownTime = Mathf.Max(0.1f, totalSeconds);
        timer = Mathf.Max(0f, remainingSeconds);
        isCooling = timer > 0f;

        if (isCooling)
        {
            if (cooldownMask != null)
            {
                cooldownMask.enabled = true;
            }

            if (cooldownText != null)
            {
                cooldownText.enabled = true;
            }

            UpdateCooldownUI();
        }
        else
        {
            FinishCooldown();
        }
    }

    public void StartCooldown()
    {
        SetCooldown(cooldownTime, cooldownTime);
    }

    private void FinishCooldown()
    {
        isCooling = false;
        timer = 0f;

        if (cooldownMask != null)
        {
            cooldownMask.fillAmount = 0f;
            cooldownMask.enabled = false;
        }

        if (cooldownText != null)
        {
            cooldownText.text = "";
            cooldownText.enabled = false;
        }
    }

    private void UpdateCooldownUI()
    {
        if (cooldownText != null)
        {
            cooldownText.text = Mathf.CeilToInt(timer).ToString();
        }

        if (cooldownMask != null)
        {
            cooldownMask.fillAmount = Mathf.Clamp01(timer / Mathf.Max(0.1f, cooldownTime));
        }
    }

    private void ResolvePlayerCuteAction()
    {
        if (!autoFindPlayerCuteAction || playerCuteAction != null)
        {
            return;
        }

        playerCuteAction = FindObjectOfType<PlayerCuteAction>();
    }
}
