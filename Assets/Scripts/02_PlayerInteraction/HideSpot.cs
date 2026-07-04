using UnityEngine;

public class HideSpot : MonoBehaviour, IHideSpot
{
    [SerializeField] private string interactionId = "HideSpot";
    [SerializeField] private bool canInteract = true;
    [SerializeField] private Transform hidePoint;
    [SerializeField] private CoreFacade coreFacade;
    [SerializeField] private HidingManager hidingManager;

    public string InteractionId => interactionId;
    public bool CanInteract
    {
        get
        {
            if (!canInteract)
            {
                return false;
            }

            if (coreFacade == null)
            {
                coreFacade = FindObjectOfType<CoreFacade>();
            }

            if (coreFacade != null)
            {
                return coreFacade.CanUseHideSpot(interactionId);
            }

            if (hidingManager == null)
            {
                hidingManager = FindObjectOfType<HidingManager>();
            }

            return hidingManager == null || hidingManager.CanUseHideSpot(interactionId);
        }
    }
    public Transform HidePoint => hidePoint;

    private void Awake()
    {
        if (hidePoint == null)
        {
            Transform existingHidePoint = transform.Find("HidePoint");
            if (existingHidePoint != null)
            {
                hidePoint = existingHidePoint;
            }
        }

        if (coreFacade == null) coreFacade = FindObjectOfType<CoreFacade>();
        if (hidingManager == null) hidingManager = FindObjectOfType<HidingManager>();
    }

    public void Interact(string actorId)
    {
        // Hide is handled by PlayerHide (F key) when this spot is the current target.
    }

    public void Consume()
    {
        canInteract = false;
    }
}
