using UnityEngine;

public class NpcCatch : MonoBehaviour
{
    [SerializeField] private NpcController npcController;
    [SerializeField] private FailManager failManager;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
            TryCatchPlayer(player);
    }

    public void TryCatchPlayer(PlayerController player)
    {
        if (player == null || player.IsHidden)
            return;

        if (npcController != null)
            npcController.OnPlayerCaught(player);

        if (failManager != null)
            failManager.HandlePlayerCaught();
    }
}
