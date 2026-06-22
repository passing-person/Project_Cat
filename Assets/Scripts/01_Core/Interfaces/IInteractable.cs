public interface IInteractable
{
    string InteractionId { get; }
    bool CanInteract { get; }

    void Interact(string actorId);
}
