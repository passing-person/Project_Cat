using UnityEngine;

[DisallowMultipleComponent]
public class LightSwitchControlledLight : MonoBehaviour
{
    [SerializeField] private string controlledLightId = "OfficePointLight";

    public string ControlledLightId => controlledLightId;

    private void Reset()
    {
        Light light = GetComponent<Light>();
        if (light != null && light.type == LightType.Directional)
        {
            Debug.LogWarning("LightSwitchControlledLight should not be placed on a Directional Light.", this);
        }
    }
}
