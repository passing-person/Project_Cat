using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldRageBarManager : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private CoreFacade coreFacade;

    [Header("Display")]
    [SerializeField] private bool autoFindCore = true;
    [SerializeField] private bool showBars = true;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.1f, 0f);
    [SerializeField] private Vector2 barSize = new Vector2(160f, 42f);
    [SerializeField] private float pollInterval = 0.05f;
    [SerializeField] private bool updateEveryFrame = true;

    private readonly Dictionary<string, RageBarView> views = new Dictionary<string, RageBarView>();
    private RectTransform canvasRect;
    private RectTransform containerRect;
    private Canvas rootCanvas;
    private float pollTimer;

    private void Awake()
    {
        updateEveryFrame = true;
        EnsureReferences();
        EnsureContainer();
    }

    private void Start()
    {
        EnsureReferences();
        EnsureContainer();
        RefreshNow();
    }

    private void LateUpdate()
    {
        // World-space UI must follow after the camera has moved.
        // Updating only on a polling interval made the rage bars look like low-FPS UI.
        if (updateEveryFrame || pollInterval <= 0f)
        {
            RefreshNow();
            return;
        }

        pollTimer -= Time.unscaledDeltaTime;
        if (pollTimer > 0f)
        {
            return;
        }

        pollTimer = Mathf.Max(0.02f, pollInterval);
        RefreshNow();
    }

    public void BindCore(CoreFacade facade)
    {
        coreFacade = facade;
        RefreshNow();
    }

    public void SetVisible(bool visible)
    {
        showBars = visible;
        if (containerRect != null)
        {
            containerRect.gameObject.SetActive(visible);
        }
    }

    public void RefreshNow()
    {
        if (!showBars)
        {
            return;
        }

        EnsureReferences();
        EnsureContainer();

        if (coreFacade == null || canvasRect == null || rootCanvas == null)
        {
            HideAll();
            return;
        }

        Camera worldCamera = Camera.main;
        if (worldCamera == null)
        {
            HideAll();
            return;
        }

        List<string> npcIds = coreFacade.GetRegisteredNpcIds();
        HashSet<string> visibleIds = new HashSet<string>();

        for (int i = 0; i < npcIds.Count; i++)
        {
            string npcId = npcIds[i];
            if (string.IsNullOrEmpty(npcId))
            {
                continue;
            }

            if (!coreFacade.TryGetNpcWorldPosition(npcId, out Vector3 worldPosition))
            {
                continue;
            }

            Vector3 screenPoint = worldCamera.WorldToScreenPoint(worldPosition + worldOffset);
            if (screenPoint.z <= 0f)
            {
                continue;
            }

            Camera uiCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, uiCamera, out Vector2 localPoint))
            {
                continue;
            }

            float rage = Mathf.Clamp(coreFacade.GetRage(npcId), 0f, 100f);
            RageBarView view = GetOrCreateView(npcId);
            view.SetActive(true);
            view.SetPosition(localPoint);
            view.SetValue(npcId, rage, GetRageColor(rage));
            visibleIds.Add(npcId);
        }

        foreach (KeyValuePair<string, RageBarView> pair in views)
        {
            if (!visibleIds.Contains(pair.Key))
            {
                pair.Value.SetActive(false);
            }
        }
    }

    private void EnsureReferences()
    {
        if (autoFindCore && coreFacade == null)
        {
            coreFacade = FindObjectOfType<CoreFacade>();
        }

        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas == null)
            {
                rootCanvas = FindObjectOfType<Canvas>();
            }
        }

        if (rootCanvas != null && canvasRect == null)
        {
            canvasRect = rootCanvas.GetComponent<RectTransform>();
        }
    }

    private void EnsureContainer()
    {
        if (containerRect != null)
        {
            return;
        }

        Transform existing = transform.Find("WorldRageBars");
        if (existing != null)
        {
            containerRect = existing as RectTransform;
        }

        if (containerRect == null)
        {
            GameObject container = new GameObject("WorldRageBars", typeof(RectTransform));
            container.transform.SetParent(transform, false);
            containerRect = container.GetComponent<RectTransform>();
        }

        StretchToParent(containerRect);
        containerRect.gameObject.SetActive(showBars);
    }

    private RageBarView GetOrCreateView(string npcId)
    {
        if (views.TryGetValue(npcId, out RageBarView existing) && existing != null)
        {
            return existing;
        }

        RageBarView created = RageBarView.Create(containerRect, npcId, barSize);
        views[npcId] = created;
        return created;
    }

    private void HideAll()
    {
        foreach (KeyValuePair<string, RageBarView> pair in views)
        {
            pair.Value.SetActive(false);
        }
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;
    }

    private static Color GetRageColor(float rage)
    {
        if (rage >= 100f) return new Color(1f, 0.05f, 0.02f, 1f);
        if (rage >= 70f) return new Color(1f, 0.4f, 0f, 1f);
        if (rage >= 40f) return new Color(1f, 0.85f, 0.1f, 1f);
        return new Color(0.25f, 0.9f, 0.35f, 1f);
    }

    private sealed class RageBarView
    {
        private readonly GameObject root;
        private readonly RectTransform rect;
        private readonly TMP_Text label;
        private readonly Image fill;

        public static RageBarView Create(RectTransform parent, string npcId, Vector2 size)
        {
            GameObject root = new GameObject("NpcRageBar_" + npcId, typeof(RectTransform), typeof(Image));
            root.transform.SetParent(parent, false);

            RectTransform rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            Image background = root.GetComponent<Image>();
            background.color = new Color(0f, 0f, 0f, 0.62f);
            background.raycastTarget = false;

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(root.transform, false);
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.45f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(6f, 0f);
            labelRect.offsetMax = new Vector2(-6f, -2f);
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.text = npcId + " 0%";
            label.fontSize = 15f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.raycastTarget = false;

            GameObject barBackgroundObject = new GameObject("BarBackground", typeof(RectTransform), typeof(Image));
            barBackgroundObject.transform.SetParent(root.transform, false);
            RectTransform barBgRect = barBackgroundObject.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0f, 0f);
            barBgRect.anchorMax = new Vector2(1f, 0f);
            barBgRect.pivot = new Vector2(0.5f, 0f);
            barBgRect.offsetMin = new Vector2(8f, 7f);
            barBgRect.offsetMax = new Vector2(-8f, 19f);
            Image barBackground = barBackgroundObject.GetComponent<Image>();
            barBackground.color = new Color(0f, 0f, 0f, 0.82f);
            barBackground.raycastTarget = false;

            GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillObject.transform.SetParent(barBackgroundObject.transform, false);
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fill = fillObject.GetComponent<Image>();
            fill.color = Color.green;
            fill.raycastTarget = false;

            return new RageBarView(root, rect, label, fill);
        }

        private RageBarView(GameObject root, RectTransform rect, TMP_Text label, Image fill)
        {
            this.root = root;
            this.rect = rect;
            this.label = label;
            this.fill = fill;
        }

        public void SetActive(bool active)
        {
            if (root.activeSelf != active)
            {
                root.SetActive(active);
            }
        }

        public void SetPosition(Vector2 anchoredPosition)
        {
            rect.anchoredPosition = anchoredPosition;
        }

        public void SetValue(string npcId, float rage, Color color)
        {
            label.text = npcId + " " + rage.ToString("0") + "%";
            fill.color = color;
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMax = new Vector2(Mathf.Clamp01(rage / 100f), 1f);
        }
    }
}
