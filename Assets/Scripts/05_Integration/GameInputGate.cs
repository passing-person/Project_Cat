using System;
using UnityEngine;

public class GameInputGate : MonoBehaviour
{
    private static int manualBlockCount;
    private static bool menuOpen;
    private static float suppressGameplayInputUntilRealtime;

    public static bool IsMenuOpen => menuOpen;
    public static bool IsGameplayInputBlocked => menuOpen || manualBlockCount > 0 || Time.unscaledTime < suppressGameplayInputUntilRealtime;
    public static bool CanUseGameplayInput => !IsGameplayInputBlocked;

    public static event Action<bool> GameplayInputBlockChanged;

    public static void SetMenuOpen(bool open)
    {
        if (menuOpen == open)
        {
            ApplyCursorState();
            return;
        }

        menuOpen = open;
        if (!open)
        {
            SuppressGameplayInputFor(0.15f);
        }
        ApplyCursorState();
        GameplayInputBlockChanged?.Invoke(IsGameplayInputBlocked);
    }

    public static void PushGameplayBlock()
    {
        bool wasBlocked = IsGameplayInputBlocked;
        manualBlockCount++;
        if (wasBlocked != IsGameplayInputBlocked)
        {
            ApplyCursorState();
            GameplayInputBlockChanged?.Invoke(IsGameplayInputBlocked);
        }
    }

    public static void PopGameplayBlock()
    {
        bool wasBlocked = IsGameplayInputBlocked;
        manualBlockCount = Mathf.Max(0, manualBlockCount - 1);
        if (wasBlocked != IsGameplayInputBlocked)
        {
            ApplyCursorState();
            GameplayInputBlockChanged?.Invoke(IsGameplayInputBlocked);
        }
    }

    public static void SuppressGameplayInputFor(float seconds)
    {
        suppressGameplayInputUntilRealtime = Mathf.Max(suppressGameplayInputUntilRealtime, Time.unscaledTime + Mathf.Max(0f, seconds));
    }

    public static void ClearGameplayBlocks()
    {
        bool wasBlocked = IsGameplayInputBlocked;
        manualBlockCount = 0;
        menuOpen = false;
        suppressGameplayInputUntilRealtime = 0f;
        ApplyCursorState();
        if (wasBlocked != IsGameplayInputBlocked)
        {
            GameplayInputBlockChanged?.Invoke(IsGameplayInputBlocked);
        }
    }

    private static void ApplyCursorState()
    {
        if (IsGameplayInputBlocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
