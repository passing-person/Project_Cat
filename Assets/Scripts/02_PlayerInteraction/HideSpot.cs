using UnityEngine;

public class HideSpot : MonoBehaviour, IHideSpot
{
    [SerializeField] private string interactionId = "HideSpot";
    [SerializeField] private bool canInteract = true;
    [SerializeField] private Transform hidePoint;

    public string InteractionId => interactionId;
    public bool CanInteract => canInteract;
    public Transform HidePoint => hidePoint;

    public void Interact(string actorId)
    {
        // Hide is handled by PlayerHide (F key) when this spot is the current target.
    }

    public void Consume()
    {
        canInteract = false;
    }
}
