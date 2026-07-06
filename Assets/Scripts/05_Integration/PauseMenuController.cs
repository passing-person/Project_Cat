using UnityEngine;

/// <summary>
/// Deprecated legacy pause menu controller.
/// It is intentionally component-only no-op and must not deactivate its GameObject,
/// because it may live on Systems with other runtime components.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    private void Awake()
    {
        enabled = false;
    }

    public void ToggleMenu() { }
    public void OpenMenu() { }
    public void CloseMenu() { }
    public void RestartScene() { }
    public void QuitGame() { }
}
