using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PlayerCuteAction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private RageManager rageManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayerAnimationController animationController;
    [SerializeField] private PlayerSfxController sfxController;

    [Header("Cute Action")]
    public float radius = 5f;
    public float rageReduction = 20f;

    [Header("Animation Lock")]
    [SerializeField] private string cuteStateName = "Cute";

    [Header("Cooldown")]
    public float cooldown = 20f;
    public float cooldownUiReportInterval = 1f;

    private float cooldownTimer;
    private float cooldownUiTimer;
    private bool wasOnCooldown;
    private bool cuteAnimationPlaying;
    private Coroutine cuteLockRoutine;

    private void Awake()
    {
        if (playerController == null) playerController = GetComponent<PlayerController>();
        if (coreFacade == null) coreFacade = FindObjectOfType<CoreFacade>();
        if (rageManager == null) rageManager = FindObjectOfType<RageManager>();
        if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
        if (animationController == null) animationController = GetComponent<PlayerAnimationController>();
        if (sfxController == null) sfxController = GetComponent<PlayerSfxController>();
    }

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
        if (cuteAnimationPlaying)
        {
            uiManager?.ShowActionBlocked("Cute action in progress");
            return;
        }

        if (playerController != null && (!playerController.IsControllable || playerController.IsHidden))
        {
            uiManager?.ShowActionBlocked("Cannot cute right now");
            return;
        }

        if (cooldownTimer > 0f)
        {
            uiManager?.ShowActionBlocked("Cute cooldown " + cooldownTimer.ToString("0.0") + "s");
            return;
        }

        List<RageResult> results = null;

        if (coreFacade != null)
            results = coreFacade.TryCuteAction(transform.position, radius, rageReduction);
        else if (rageManager != null)
            results = rageManager.ReduceRageAround(transform.position, radius, rageReduction, excludeSecurity: true);

        if (results == null || results.Count == 0)
        {
            uiManager?.ShowActionBlocked("No NPC within " + radius.ToString("0") + "m");
            return;
        }

        cooldownTimer = cooldown;
        cooldownUiTimer = 0f;
        uiManager?.SetCuteCooldown(cooldownTimer, cooldown);
        uiManager?.ShowCuteApplied(results, rageReduction);

        animationController?.PlayCute();
        sfxController?.PlayCute();
        cuteLockRoutine = StartCoroutine(LockControlForCuteAnimation());
    }

    private IEnumerator LockControlForCuteAnimation()
    {
        cuteAnimationPlaying = true;
        playerController?.AddTemporaryControlLock();

        if (animationController != null)
            yield return animationController.WaitForStateToFinish(cuteStateName);

        playerController?.RemoveTemporaryControlLock();
        cuteAnimationPlaying = false;
        cuteLockRoutine = null;
    }

    private void OnDisable()
    {
        if (cuteLockRoutine != null)
        {
            StopCoroutine(cuteLockRoutine);
            cuteLockRoutine = null;
        }

        if (cuteAnimationPlaying)
        {
            playerController?.RemoveTemporaryControlLock();
            cuteAnimationPlaying = false;
        }
    }
}
