using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.Escape;

    [Header("Pause")]
    [SerializeField] private bool pauseTimeScale = true;
    [SerializeField] private GameObject menuRoot;

    private float previousTimeScale = 1f;
    private bool isOpen;

    private void Awake()
    {
        EnsureMenuUi();
        CloseMenuImmediate();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMenu();
        }
    }

    private void OnDisable()
    {
        if (isOpen)
        {
            CloseMenuImmediate();
        }
    }

    public void ToggleMenu()
    {
        if (isOpen)
        {
            CloseMenu();
        }
        else
        {
            OpenMenu();
        }
    }

    public void OpenMenu()
    {
        if (isOpen)
        {
            return;
        }

        isOpen = true;
        if (menuRoot != null)
        {
            menuRoot.SetActive(true);
        }

        if (pauseTimeScale)
        {
            previousTimeScale = Time.timeScale <= 0f ? 1f : Time.timeScale;
            Time.timeScale = 0f;
        }

        GameInputGate.SetMenuOpen(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseMenu()
    {
        if (!isOpen)
        {
            return;
        }

        CloseMenuImmediate();
    }

    public void RestartScene()
    {
        CloseMenuImmediate();
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid())
        {
            SceneManager.LoadScene(activeScene.name);
        }
    }

    public void QuitGame()
    {
        CloseMenuImmediate();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void CloseMenuImmediate()
    {
        isOpen = false;
        if (menuRoot != null)
        {
            menuRoot.SetActive(false);
        }

        if (pauseTimeScale)
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }

        GameInputGate.SetMenuOpen(false);
    }

    private void EnsureMenuUi()
    {
        if (menuRoot != null)
        {
            return;
        }

        EnsureEventSystem();

        GameObject canvasObject = new GameObject("PauseMenuCanvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("PauseMenuPanel");
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.72f);

        CreateLabel(panel.transform, "PAUSED", new Vector2(0f, 150f), 42);
        CreateButton(panel.transform, "Resume", new Vector2(0f, 55f), CloseMenu);
        CreateButton(panel.transform, "Restart", new Vector2(0f, -15f), RestartScene);
        CreateButton(panel.transform, "Quit", new Vector2(0f, -85f), QuitGame);
        CreateLabel(panel.transform, "ESC: Resume", new Vector2(0f, -160f), 18);

        menuRoot = canvasObject;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static Text CreateLabel(Transform parent, string text, Vector2 anchoredPosition, int fontSize)
    {
        GameObject labelObject = new GameObject(text + "Label");
        labelObject.transform.SetParent(parent, false);
        RectTransform rect = labelObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(420f, 60f);
        rect.anchoredPosition = anchoredPosition;

        Text label = labelObject.AddComponent<Text>();
        label.text = text;
        label.alignment = TextAnchor.MiddleCenter;
        label.font = GetDefaultFont();
        label.fontSize = fontSize;
        label.color = Color.white;
        return label;
    }

    private static Button CreateButton(Transform parent, string text, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(text + "Button");
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(260f, 48f);
        rect.anchoredPosition = anchoredPosition;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.92f);

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        Text label = CreateLabel(buttonObject.transform, text, Vector2.zero, 22);
        label.color = Color.black;
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        labelRect.anchoredPosition = Vector2.zero;

        return button;
    }

    private static Font GetDefaultFont()
    {
        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
