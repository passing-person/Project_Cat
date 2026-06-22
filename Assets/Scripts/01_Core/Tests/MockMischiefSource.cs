using UnityEngine;

public class MockMischiefSource : MonoBehaviour, IMischiefTarget
{
    [Header("Mock Target")]
    public string actorId = "TestCat";
    public MischiefTargetData targetData;
    public bool canInteract = true;

    public string InteractionId => targetData != null ? targetData.targetId : "MockTarget";
    public bool CanInteract => canInteract;
    public MischiefType MischiefType => targetData != null ? targetData.mischiefType : MischiefType.Stomp;
    public int BaseScore => targetData != null ? targetData.instantScoreBonus : 0;
    public float BaseRageAmount => targetData != null ? targetData.baseRageAmount : 5f;
    public float RageRadius => targetData != null ? targetData.rageRadius : 5f;
    public string PrimaryNpcId => targetData != null ? targetData.primaryNpcId : "Supervisor";

    public void Interact(string newActorId)
    {
        actorId = newActorId;
    }

    public MischiefContext CreateContext(string newActorId)
    {
        return new MischiefContext(
            newActorId,
            InteractionId,
            MischiefType,
            BaseScore,
            BaseRageAmount,
            transform.position,
            RageRadius,
            PrimaryNpcId);
    }
}
