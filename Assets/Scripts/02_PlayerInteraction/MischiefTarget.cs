using UnityEngine;

public class MischiefTarget : MonoBehaviour, IMischiefTarget
{
    [SerializeField] private MischiefTargetData data;
    [SerializeField] private bool canInteract = true;

    [Header("Fallback Values")]
    [SerializeField] private string interactionId = "MischiefTarget";
    [SerializeField] private MischiefType mischiefType = MischiefType.Stomp;
    [SerializeField] private int instantScoreBonus = 0;
    [SerializeField] private float baseRageAmount = 5f;
    [SerializeField] private float rageRadius = 5f;
    [SerializeField] private string primaryNpcId = "";

    public string InteractionId => data != null ? data.targetId : interactionId;
    public bool CanInteract => canInteract;
    public MischiefType MischiefType => data != null ? data.mischiefType : mischiefType;
    public int BaseScore => data != null ? data.instantScoreBonus : instantScoreBonus;
    public float BaseRageAmount => data != null ? data.baseRageAmount : baseRageAmount;
    public float RageRadius => data != null ? data.rageRadius : rageRadius;
    public string PrimaryNpcId => data != null ? data.primaryNpcId : primaryNpcId;

    public void Interact(string actorId)
    {
        // Interaction is intentionally light. Actual mischief should be triggered by PlayerMischiefAction.
    }

    public MischiefContext CreateContext(string actorId)
    {
        return new MischiefContext(
            actorId,
            InteractionId,
            MischiefType,
            BaseScore,
            BaseRageAmount,
            transform.position,
            RageRadius,
            PrimaryNpcId
        );
    }

    public void LockTarget()
    {
        canInteract = false;
    }

    public void UnlockTarget()
    {
        canInteract = true;
    }
}
