using UnityEngine;

public enum SceneLightRoleType
{
    Unspecified,
    AmbientMainDirectional,
    OfficeSwitchControlled,
    SecuritySpotlight,
    Decorative
}

[DisallowMultipleComponent]
public class SceneLightRole : MonoBehaviour
{
    [SerializeField] private SceneLightRoleType role = SceneLightRoleType.Unspecified;

    public SceneLightRoleType Role => role;

    public void SetRole(SceneLightRoleType newRole)
    {
        role = newRole;
    }

    public bool CanBeControlledByOfficeSwitch()
    {
        return role == SceneLightRoleType.OfficeSwitchControlled;
    }
}
