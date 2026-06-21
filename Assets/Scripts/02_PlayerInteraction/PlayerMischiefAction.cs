using UnityEngine;

public class PlayerMischiefAction : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private MischiefManager mischiefManager;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryPerformMischief();
    }

    public void TryPerformMischief()
    {
        if (playerInteraction == null || mischiefManager == null)
            return;

        IMischiefTarget target = playerInteraction.CurrentTarget as IMischiefTarget;
        if (target == null || !target.CanInteract)
            return;

        string actorId = playerController != null ? playerController.PlayerId : "Player";
        MischiefContext context = target.CreateContext(actorId);
        mischiefManager.ApplyMischief(context);
    }
}
