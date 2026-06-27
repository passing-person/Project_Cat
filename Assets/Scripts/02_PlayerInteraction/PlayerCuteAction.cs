using System.Collections.Generic;
using UnityEngine;

public class PlayerCuteAction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private RageManager rageManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerSfxController sfxController;

    [Header("Cute Action")]
    public float radius = 4f;
    public float rageReduction = 20f;

    [Header("Cooldown")]
    public float cooldown = 5f;
    public float cooldownUiReportInterval = 1f;

    private float cooldownTimer;
    private float cooldownUiTimer;
    private bool wasOnCooldown;

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            wasOnCooldown = true;
            cooldownTimer -= Time.deltaTime;
            cooldownUiTimer -= Time.deltaTime;

            if (cooldownUiTimer <= 0f)
            {
                cooldownUiTimer = cooldownUiReportInterval;
                uiManager?.SetCuteCooldown(cooldownTimer, cooldown);
            }
        }
        else if (wasOnCooldown)
        {
            wasOnCooldown = false;
            cooldownUiTimer = 0f;
            uiManager?.HideCuteCooldown();
        }

        if (Input.GetKeyDown(KeyCode.Q))
            TryCuteAction();
    }

    public void TryCuteAction()
    {
        if (cooldownTimer > 0f)
            return;

        List<RageResult> results = null;

        if (coreFacade != null)
            results = coreFacade.TryCuteAction(transform.position, radius, rageReduction);
        else if (rageManager != null)
            results = rageManager.ReduceRageAround(transform.position, radius, rageReduction, excludeSecurity: true);

        if (results == null || results.Count == 0)
            return;

        cooldownTimer = cooldown;
        cooldownUiTimer = 0f;
        uiManager?.SetCuteCooldown(cooldownTimer, cooldown);

        animationController?.PlayCute();
        sfxController?.PlayCute();
    }
}
