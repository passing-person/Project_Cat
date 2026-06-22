public interface IMischiefTarget : IInteractable
{
    // Keep this interface compatible with the previous PlayerInteraction implementation.
    // Target identity is already provided by IInteractable.InteractionId.
    MischiefType MischiefType { get; }

    // Optional immediate score. Core currently uses time-based score by default,
    // but this value can be used if MischiefManager.grantInstantScoreFromContext is enabled.
    int BaseScore { get; }

    // Rage amount added when this target is used.
    float BaseRageAmount { get; }

    // Radius used by RageManager to find affected NPCs.
    float RageRadius { get; }

    // Optional owner/specific NPC target. Empty string means range-only targeting.
    string PrimaryNpcId { get; }

    MischiefContext CreateContext(string actorId);
}
