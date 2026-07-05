using UnityEngine;

[DisallowMultipleComponent]
public class MischiefControlledLight : MonoBehaviour
{
    [SerializeField] private string targetId = "LightSwitch";
    [SerializeField] private bool allowSwitchControl = true;

    public string TargetId => targetId;
    public bool AllowSwitchControl => allowSwitchControl;

    public void Configure(string controlledTargetId, bool canBeControlled)
    {
        targetId = string.IsNullOrWhiteSpace(controlledTargetId) ? "LightSwitch" : controlledTargetId;
        allowSwitchControl = canBeControlled;
    }

    public bool CanBeControlledBy(string requesterTargetId)
    {
        if (!allowSwitchControl)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            return true;
        }

        return string.IsNullOrWhiteSpace(requesterTargetId) || targetId == requesterTargetId;
    }
}
