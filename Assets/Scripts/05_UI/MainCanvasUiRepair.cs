using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the designed MainCanvas active and disables only old generated UI.
/// This class must never disable Systems or the MainCanvas root.
/// </summary>
public static class MainCanvasUiRepair
{
    public static void Repair(GameObject hint)
    {
        GameObject mainCanvas = FindMainCanvasRoot(hint);
        if (mainCanvas == null)
        {
            return;
        }

        ActivateMainCanvas(mainCanvas);
        DisableLegacyImmediateUi();
        DisableLegacyPauseControllers();
        DisableLegacyCanvases(mainCanvas);
        NormalizePauseMenus(mainCanvas);
    }

    public static bool IsUnderMainCanvas(GameObject obj)
    {
        if (obj == null)
        {
            return false;
        }

        Transform current = obj.transform;
        while (current != null)
        {
            if (current.name == "MainCanvas")
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    public static GameObject FindMainCanvasRoot(GameObject hint = null)
    {
        GameObject hintedRoot = FindMainCanvasRootFromTransform(hint != null ? hint.transform : null);
        if (hintedRoot != null)
        {
            return hintedRoot;
        }

        DesignedUICoreConnector[] connectors = Object.FindObjectsOfType<DesignedUICoreConnector>(true);
        for (int i = 0; i < connectors.Length; i++)
        {
            if (connectors[i] == null)
            {
                continue;
            }

            GameObject root = FindMainCanvasRootFromTransform(connectors[i].transform);
            if (root != null)
            {
                return root;
            }
        }

        return GameObject.Find("MainCanvas");
    }

    private static GameObject FindMainCanvasRootFromTransform(Transform transform)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "MainCanvas")
            {
                return current.gameObject;
            }

            current = current.parent;
        }

        return null;
    }

    private static void ActivateMainCanvas(GameObject mainCanvas)
    {
        if (mainCanvas == null)
        {
            return;
        }

        mainCanvas.SetActive(true);
        mainCanvas.transform.localScale = Vector3.one;

        RectTransform rootRect = mainCanvas.GetComponent<RectTransform>();
        if (rootRect != null)
        {
            rootRect.localScale = Vector3.one;
        }

        Canvas[] canvases = mainCanvas.GetComponentsInChildren<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
            {
                continue;
            }

            canvas.gameObject.SetActive(true);
            canvas.enabled = true;
            if (canvas.sortingOrder < 4500)
            {
                canvas.sortingOrder = 4500 + i;
            }
        }

        GraphicRaycaster[] raycasters = mainCanvas.GetComponentsInChildren<GraphicRaycaster>(true);
        for (int i = 0; i < raycasters.Length; i++)
        {
            if (raycasters[i] != null)
            {
                raycasters[i].enabled = true;
            }
        }
    }

    private static void DisableLegacyImmediateUi()
    {
        UIManager[] managers = Object.FindObjectsOfType<UIManager>(true);
        for (int i = 0; i < managers.Length; i++)
        {
            if (managers[i] != null)
            {
                managers[i].DisableImmediateModeOverlay();
            }
        }
    }

    private static void DisableLegacyPauseControllers()
    {
        PauseMenuController[] controllers = Object.FindObjectsOfType<PauseMenuController>(true);
        for (int i = 0; i < controllers.Length; i++)
        {
            PauseMenuController controller = controllers[i];
            if (controller != null)
            {
                controller.enabled = false;
            }
        }
    }

    private static void DisableLegacyCanvases(GameObject mainCanvas)
    {
        Canvas[] canvases = Object.FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas canvas = canvases[i];
            if (canvas == null)
            {
                continue;
            }

            GameObject obj = canvas.gameObject;
            if (obj == null || BelongsTo(obj, mainCanvas))
            {
                continue;
            }

            string name = obj.name.ToLowerInvariant();
            if (name.Contains("pausemenucanvas") || name.Contains("pause menu canvas") || name.Contains("designeduicanvas") || name.Contains("legacy") || name.Contains("generated"))
            {
                obj.SetActive(false);
            }
        }
    }

    private static void NormalizePauseMenus(GameObject mainCanvas)
    {
        PauseMenu[] pauseMenus = Object.FindObjectsOfType<PauseMenu>(true);
        PauseMenu primary = null;

        for (int i = 0; i < pauseMenus.Length; i++)
        {
            PauseMenu menu = pauseMenus[i];
            if (menu == null || !BelongsTo(menu.gameObject, mainCanvas))
            {
                continue;
            }

            if (primary == null || menu.gameObject.name.Contains("Canvas-PauseMenu"))
            {
                primary = menu;
            }
        }

        for (int i = 0; i < pauseMenus.Length; i++)
        {
            PauseMenu menu = pauseMenus[i];
            if (menu == null)
            {
                continue;
            }

            bool isPrimary = menu == primary;
            menu.enabled = isPrimary;
            if (!isPrimary && menu.pausePanel != null)
            {
                menu.pausePanel.SetActive(false);
            }
        }

        if (primary != null)
        {
            primary.enabled = true;
            primary.EnsureInitialState();
        }
    }

    private static bool BelongsTo(GameObject obj, GameObject root)
    {
        if (obj == null || root == null)
        {
            return false;
        }

        return obj == root || obj.transform.IsChildOf(root.transform);
    }
}
