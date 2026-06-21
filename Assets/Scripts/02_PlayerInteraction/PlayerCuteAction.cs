using UnityEngine;

public class PlayerCuteAction : MonoBehaviour
{
    [SerializeField] private RageManager rageManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float rageReduction = 20f;
    [SerializeField] private float cooldown = 10f;

    private float cooldownTimer;

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;

            if (uiManager != null)
                uiManager.SetCuteCooldown(Mathf.Clamp01(cooldownTimer / cooldown));
        }

        if (Input.GetKeyDown(KeyCode.Q))
            TryCuteAction();
    }

    public void TryCuteAction()
    {
        if (cooldownTimer > 0f || rageManager == null)
            return;

        rageManager.ReduceRageAround(transform.position, radius, rageReduction);
        cooldownTimer = cooldown;

        // TODO: Play cute animation and sound here.
    }
}
