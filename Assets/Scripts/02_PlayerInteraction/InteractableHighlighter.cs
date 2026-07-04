using UnityEngine;

public class InteractableHighlighter : MonoBehaviour
{
    [SerializeField] private GameObject highlightObject;
    [SerializeField] private bool autoCreateMarker = true;
    [SerializeField] private bool tintRenderers = true;
    [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.15f, 1f);
    [SerializeField] private float markerHeight = 0.75f;
    [SerializeField] private float markerPulseSpeed = 5f;
    [SerializeField] private float markerPulseScale = 0.15f;
    [SerializeField] private string keyLabel = "E / LMB";

    private Renderer[] renderers;
    private Color[] originalColors;
    private GameObject runtimeMarker;
    private TextMesh runtimeLabel;
    private bool isHighlighted;
    private float pulseTimer;

    private void Awake()
    {
        CacheRenderers();
        if (autoCreateMarker && highlightObject == null)
        {
            CreateRuntimeMarker();
        }

        HideHighlight();
    }

    private void Update()
    {
        if (!isHighlighted)
        {
            return;
        }

        if (runtimeMarker != null)
        {
            pulseTimer += Time.deltaTime * markerPulseSpeed;
            float scale = 1f + Mathf.Sin(pulseTimer) * markerPulseScale;
            runtimeMarker.transform.localScale = Vector3.one * scale;
        }

        if (runtimeLabel != null && Camera.main != null)
        {
            Vector3 toCamera = runtimeLabel.transform.position - Camera.main.transform.position;
            if (toCamera.sqrMagnitude > 0.001f)
            {
                runtimeLabel.transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
            }
        }
    }

    public void ShowHighlight()
    {
        isHighlighted = true;
        if (highlightObject != null) highlightObject.SetActive(true);
        if (runtimeLabel != null) runtimeLabel.gameObject.SetActive(true);
        if (tintRenderers) ApplyTint();
    }

    public void HideHighlight()
    {
        isHighlighted = false;
        if (highlightObject != null) highlightObject.SetActive(false);
        if (runtimeLabel != null) runtimeLabel.gameObject.SetActive(false);
        RestoreTint();
    }

    public void SetKeyLabel(string label)
    {
        keyLabel = string.IsNullOrEmpty(label) ? "E / LMB" : label;
        if (runtimeLabel != null)
        {
            runtimeLabel.text = keyLabel;
        }
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer != null && targetRenderer.sharedMaterial != null && targetRenderer.sharedMaterial.HasProperty("_Color"))
            {
                originalColors[i] = targetRenderer.sharedMaterial.color;
            }
            else
            {
                originalColors[i] = Color.white;
            }
        }
    }

    private void CreateRuntimeMarker()
    {
        runtimeMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        runtimeMarker.name = "InteractionMarker";
        runtimeMarker.transform.SetParent(transform, false);
        runtimeMarker.transform.localPosition = Vector3.up * markerHeight;
        runtimeMarker.transform.localScale = Vector3.one * 0.2f;

        Collider markerCollider = runtimeMarker.GetComponent<Collider>();
        if (markerCollider != null)
        {
            Destroy(markerCollider);
        }

        Renderer markerRenderer = runtimeMarker.GetComponent<Renderer>();
        if (markerRenderer != null && markerRenderer.material != null)
        {
            markerRenderer.material.color = highlightColor;
            if (markerRenderer.material.HasProperty("_EmissionColor"))
            {
                markerRenderer.material.EnableKeyword("_EMISSION");
                markerRenderer.material.SetColor("_EmissionColor", highlightColor * 0.6f);
            }
        }

        GameObject labelObject = new GameObject("InteractionKeyLabel");
        labelObject.transform.SetParent(transform, false);
        labelObject.transform.localPosition = Vector3.up * (markerHeight + 0.26f);
        runtimeLabel = labelObject.AddComponent<TextMesh>();
        runtimeLabel.text = keyLabel;
        runtimeLabel.anchor = TextAnchor.MiddleCenter;
        runtimeLabel.alignment = TextAlignment.Center;
        runtimeLabel.characterSize = 0.16f;
        runtimeLabel.fontSize = 72;
        runtimeLabel.color = Color.white;

        highlightObject = runtimeMarker;
    }

    private void ApplyTint()
    {
        if (renderers == null)
        {
            CacheRenderers();
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null || targetRenderer.material == null || !targetRenderer.material.HasProperty("_Color"))
            {
                continue;
            }

            if (runtimeMarker != null && targetRenderer.gameObject == runtimeMarker)
            {
                continue;
            }

            targetRenderer.material.color = Color.Lerp(originalColors[i], highlightColor, 0.45f);
        }
    }

    private void RestoreTint()
    {
        if (renderers == null || originalColors == null)
        {
            return;
        }

        for (int i = 0; i < renderers.Length && i < originalColors.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null || targetRenderer.material == null || !targetRenderer.material.HasProperty("_Color"))
            {
                continue;
            }

            if (runtimeMarker != null && targetRenderer.gameObject == runtimeMarker)
            {
                continue;
            }

            targetRenderer.material.color = originalColors[i];
        }
    }
}
