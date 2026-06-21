public interface IMischiefTarget : IInteractable
{
    MischiefType MischiefType { get; }
    int BaseScore { get; }
    float BaseRageAmount { get; }
    float RageRadius { get; }
    string PrimaryNpcId { get; }

    MischiefContext CreateContext(string actorId);
}
