using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private UIManager uiManager;

    public IInteractable CurrentTarget { get; private set; }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            TryInteract();
    }

    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable == null || !interactable.CanInteract)
            return;

        SetCurrentTarget(interactable);
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && interactable == CurrentTarget)
            ClearCurrentTarget();
    }

    public void SetCurrentTarget(IInteractable target)
    {
        CurrentTarget = target;

        if (uiManager != null && target != null)
            uiManager.ShowPrompt("Press E to interact");
    }

    public void ClearCurrentTarget()
    {
        CurrentTarget = null;

        if (uiManager != null)
            uiManager.HidePrompt();
    }

    public void TryInteract()
    {
        if (CurrentTarget == null || !CurrentTarget.CanInteract)
            return;

        string actorId = playerController != null ? playerController.PlayerId : "Player";
        CurrentTarget.Interact(actorId);
    }
}
