using System.Collections;
using UnityEngine;

public class WaterSplatEffect : MonoBehaviour
{
    [Header("Listen Target")]
    [Tooltip("Drag the interactable object here. Only this object's interaction triggers the splat.")]
    [SerializeField] private GameObject listenTarget;

    [Header("Trigger")]
    [Tooltip("If enabled, the splat only plays when Play() is called manually.")]
    [SerializeField] private bool manualOnly;

    [Header("Timing")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float holdDuration = 10f;
    [SerializeField] private float fadeOutDuration = 1f;

    [Header("Visual")]
    [SerializeField] private Renderer splatRenderer;
    [SerializeField] private bool hideOnStart = true;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

    private Color baseRgb = Color.white;
    private Coroutine playRoutine;
    private IWaterSplatEffectTrigger triggerBinding;
    private MischiefWorldEventReporter worldEventReporter;
    private bool wasWorldEventActive;
    private bool isPlaying;

    public GameObject ListenTarget => listenTarget;
    public bool IsPlaying => isPlaying;

    private void Awake()
    {
        if (splatRenderer == null)
        {
            splatRenderer = GetComponentInChildren<Renderer>();
        }

        CacheBaseColor();
        ResolveListenTargetBinding();

        if (hideOnStart)
        {
            SetAlpha(0f);
            SetRendererVisible(false);
        }
    }

    private void OnEnable()
    {
        ResolveListenTargetBinding();
        SubscribeTrigger();
        wasWorldEventActive = worldEventReporter != null && worldEventReporter.IsWorldEventActive;
    }

    private void OnDisable()
    {
        UnsubscribeTrigger();
        StopPlayback();
    }

    private void Update()
    {
        if (manualOnly || worldEventReporter == null || isPlaying)
        {
            return;
        }

        bool isActive = worldEventReporter.IsWorldEventActive;
        if (isActive && !wasWorldEventActive)
        {
            Play();
        }

        wasWorldEventActive = isActive;
    }

    public void Play()
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
        }

        playRoutine = StartCoroutine(PlayRoutine());
    }

    public void Stop()
    {
        StopPlayback();
        SetAlpha(0f);
        SetRendererVisible(false);
    }

    private void ResolveListenTargetBinding()
    {
        worldEventReporter = null;
        triggerBinding = null;

        if (listenTarget == null)
        {
            return;
        }

        worldEventReporter = listenTarget.GetComponent<MischiefWorldEventReporter>();
        if (worldEventReporter == null)
        {
            worldEventReporter = listenTarget.GetComponentInChildren<MischiefWorldEventReporter>(true);
        }

        MonoBehaviour[] behaviours = listenTarget.GetComponentsInChildren<MonoBehaviour>(true);
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IWaterSplatEffectTrigger trigger)
            {
                triggerBinding = trigger;
                break;
            }
        }
    }

    private void SubscribeTrigger()
    {
        if (manualOnly || triggerBinding == null)
        {
            return;
        }

        triggerBinding.WaterSplatTriggered -= HandleListenTargetTriggered;
        triggerBinding.WaterSplatTriggered += HandleListenTargetTriggered;
    }

    private void UnsubscribeTrigger()
    {
        if (triggerBinding == null)
        {
            return;
        }

        triggerBinding.WaterSplatTriggered -= HandleListenTargetTriggered;
    }

    private void HandleListenTargetTriggered()
    {
        Play();
    }

    private IEnumerator PlayRoutine()
    {
        isPlaying = true;
        SetRendererVisible(true);

        yield return Fade(0f, 1f, fadeInDuration);
        yield return new WaitForSeconds(Mathf.Max(0f, holdDuration));
        yield return Fade(1f, 0f, fadeOutDuration);

        SetAlpha(0f);
        SetRendererVisible(false);
        isPlaying = false;
        playRoutine = null;
    }

    private IEnumerator Fade(float fromAlpha, float toAlpha, float duration)
    {
        if (duration <= 0f)
        {
            SetAlpha(toAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(fromAlpha, toAlpha, t));
            yield return null;
        }

        SetAlpha(toAlpha);
    }

    private void StopPlayback()
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        isPlaying = false;
    }

    private void CacheBaseColor()
    {
        if (splatRenderer == null || splatRenderer.sharedMaterial == null)
        {
            return;
        }

        Material material = splatRenderer.sharedMaterial;
        if (material.HasProperty(BaseColorId))
        {
            baseRgb = material.GetColor(BaseColorId);
        }
        else if (material.HasProperty(ColorId))
        {
            baseRgb = material.GetColor(ColorId);
        }

        baseRgb.a = 1f;
    }

    private void SetAlpha(float alpha)
    {
        if (splatRenderer == null)
        {
            return;
        }

        float clampedAlpha = Mathf.Clamp01(alpha);
        Color color = new Color(baseRgb.r, baseRgb.g, baseRgb.b, clampedAlpha);

        splatRenderer.GetPropertyBlock(propertyBlock);

        if (splatRenderer.sharedMaterial != null)
        {
            if (splatRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                propertyBlock.SetColor(BaseColorId, color);
            }
            else if (splatRenderer.sharedMaterial.HasProperty(ColorId))
            {
                propertyBlock.SetColor(ColorId, color);
            }
        }

        splatRenderer.SetPropertyBlock(propertyBlock);
    }

    private void SetRendererVisible(bool visible)
    {
        if (splatRenderer != null)
        {
            splatRenderer.enabled = visible;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (listenTarget == null)
        {
            return;
        }

        ResolveListenTargetBinding();
    }
#endif
}
