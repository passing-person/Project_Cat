using UnityEngine;

public class PlayerBootstrap : MonoBehaviour
{
    [SerializeField] private bool autoResolveReferences = true;

    private void Awake()
    {
        if (!autoResolveReferences)
            return;

        ResolvePlayerComponents();
        ResolveSceneManagers();
    }

    private void ResolvePlayerComponents()
    {
        EnsureComponent<PlayerController>();
        EnsureComponent<PlayerAnimationController>();
        EnsureComponent<PlayerSfxController>();
        EnsureComponent<PlayerInteraction>();
        EnsureComponent<PlayerMischiefAction>();
        EnsureComponent<PlayerCuteAction>();
        EnsureComponent<PlayerHide>();
        EnsureComponent<PlayerMovement>();
    }

    private void ResolveSceneManagers()
    {
        SetPrivateField(GetComponent<PlayerMischiefAction>(), "mischiefManager", FindObjectOfType<MischiefManager>());
        SetPrivateField(GetComponent<PlayerCuteAction>(), "coreFacade", FindObjectOfType<CoreFacade>());
        SetPrivateField(GetComponent<PlayerCuteAction>(), "rageManager", FindObjectOfType<RageManager>());
        SetPrivateField(GetComponent<PlayerCuteAction>(), "uiManager", FindObjectOfType<UIManager>());
        SetPrivateField(GetComponent<PlayerHide>(), "coreFacade", FindObjectOfType<CoreFacade>());
        SetPrivateField(GetComponent<PlayerHide>(), "hidingManager", FindObjectOfType<HidingManager>());
        SetPrivateField(GetComponent<PlayerHide>(), "uiManager", FindObjectOfType<UIManager>());
        SetPrivateField(GetComponent<PlayerInteraction>(), "uiManager", FindObjectOfType<UIManager>());
        SetPrivateField(GetComponent<PlayerInteraction>(), "playerController", GetComponent<PlayerController>());
        SetPrivateField(GetComponent<PlayerSfxController>(), "audioManager", FindObjectOfType<AudioManager>());
        SetPrivateField(GetComponent<PlayerMovement>(), "playerController", GetComponent<PlayerController>());
        SetPrivateField(GetComponent<PlayerMovement>(), "animationController", GetComponent<PlayerAnimationController>());
        SetPrivateField(GetComponent<PlayerMovement>(), "sfxController", GetComponent<PlayerSfxController>());
        SetPrivateField(GetComponent<PlayerMischiefAction>(), "playerController", GetComponent<PlayerController>());
        SetPrivateField(GetComponent<PlayerMischiefAction>(), "playerInteraction", GetComponent<PlayerInteraction>());
        SetPrivateField(GetComponent<PlayerHide>(), "playerController", GetComponent<PlayerController>());
        SetPrivateField(GetComponent<PlayerHide>(), "playerInteraction", GetComponent<PlayerInteraction>());
    }

    private void EnsureComponent<T>() where T : Component
    {
        if (GetComponent<T>() == null)
            gameObject.AddComponent<T>();
    }

    private static void SetPrivateField<T>(T component, string fieldName, Object value) where T : MonoBehaviour
    {
        if (component == null || value == null)
            return;

        var field = typeof(T).GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (field == null)
            return;

        object current = field.GetValue(component);
        if (current == null)
            field.SetValue(component, value);
    }
}
