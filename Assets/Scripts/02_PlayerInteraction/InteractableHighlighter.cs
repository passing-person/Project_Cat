using UnityEngine;

public class InteractableHighlighter : MonoBehaviour
{
    [SerializeField] private GameObject highlightObject;
    [SerializeField] private bool autoCreateMarker = true;
    [SerializeField] private bool tintRenderers = true;
    [SerializeField] private Color highlightColor = new Color(1f, 0.92f, 0.15f, 1f);
    [SerializeField] private float markerHeight = 0.65f;
    [SerializeField] private float markerPulseSpeed = 5f;
    [SerializeField] private float markerPulseScale = 0.15f;

    private Renderer[] renderers;
    private Color[] originalColors;
    private GameObject runtimeMarker;
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
        if (!isHighlighted || runtimeMarker == null)
        {
            return;
        }

        pulseTimer += Time.deltaTime * markerPulseSpeed;
        float scale = 1f + Mathf.Sin(pulseTimer) * markerPulseScale;
        runtimeMarker.transform.localScale = Vector3.one * scale;
    }

    public void ShowHighlight()
    {
        isHighlighted = true;

        if (highlightObject != null)
        {
            highlightObject.SetActive(true);
        }

        if (tintRenderers)
        {
            ApplyTint();
        }
    }

    public void HideHighlight()
    {
        isHighlighted = false;

        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }

        RestoreTint();
    }

    private void CacheRenderers()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
            {
                originalColors[i] = renderer.sharedMaterial.color;
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
        runtimeMarker.transform.localScale = Vector3.one * 0.18f;

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
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.material == null || !renderer.material.HasProperty("_Color"))
            {
                continue;
            }

            if (runtimeMarker != null && renderer.gameObject == runtimeMarker)
            {
                continue;
            }

            renderer.material.color = Color.Lerp(originalColors[i], highlightColor, 0.45f);
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
            Renderer renderer = renderers[i];
            if (renderer == null || renderer.material == null || !renderer.material.HasProperty("_Color"))
            {
                continue;
            }

            if (runtimeMarker != null && renderer.gameObject == runtimeMarker)
            {
                continue;
            }

            renderer.material.color = originalColors[i];
        }
    }
}
