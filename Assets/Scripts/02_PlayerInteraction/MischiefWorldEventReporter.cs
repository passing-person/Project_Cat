using System.Collections;
using UnityEngine;

public class MischiefWorldEventReporter : MonoBehaviour, IMischiefWorldEventTarget
{
    [Header("Core")]
    [SerializeField] private CoreFacade coreFacade;

    [Header("Event")]
    [SerializeField] private MischiefWorldEventType eventType = MischiefWorldEventType.Auto;
    [SerializeField] private MischiefWorldEventResolveMode resolveMode = MischiefWorldEventResolveMode.None;
    [SerializeField] private string targetIdOverride = "";
    [SerializeField] private string preferredNpcId = "";
    [SerializeField] private NpcType preferredNpcType = NpcType.Special;
    [SerializeField] private bool disableTargetAfterNpcResponse = false;

    [Header("Light Event")]
    [SerializeField] private bool controlLightsLocally = false;
    [SerializeField] private bool lightMustBeOffToStart = true;
    [SerializeField] private bool turnLightOffOnComplete = true;
    [SerializeField] private Light[] controlledLights;

    [Header("Broadcast / SFX Event")]
    [SerializeField] private bool autoCompleteOnStart = false;
    [SerializeField] private float autoCompleteCooldown = 4f;
    [SerializeField] private string startSfxId = "";
    [SerializeField] private string completeSfxId = "";
    [SerializeField] private AudioManager audioManager;

    [Header("Visual State")]
    [SerializeField] private Renderer[] tintRenderers;
    [SerializeField] private Color availableTint = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color activeEventTint = new Color(1f, 0.85f, 0.15f, 1f);
    [SerializeField] private Color disabledTint = new Color(0.15f, 0.15f, 0.15f, 1f);

    private bool lightsEnabled = false;
    private bool eventActive = false;
    private bool registered = false;

    public string WorldEventTargetId => ResolveTargetId();
    public MischiefWorldEventType WorldEventType => ResolveEventTypeFromTargetId(WorldEventTargetId);
    public bool IsWorldEventActive => eventActive;

    private void Awake()
    {
        ResolveCoreFacade();
        CacheLightState();
    }

    private void OnEnable()
    {
        RegisterTargetIfPossible();
    }

    private void Start()
    {
        ResolveCoreFacade();
        RegisterTargetIfPossible();
        ApplyAvailableVisualState();
    }

    private void OnDisable()
    {
        if (coreFacade != null && registered)
        {
            coreFacade.UnregisterMischiefWorldEventTarget(this);
        }

        registered = false;
    }

    public void Configure(MischiefWorldEventType type, MischiefWorldEventResolveMode mode, bool disableAfterNpcResponse)
    {
        Configure(type, mode, disableAfterNpcResponse, false, 0f, string.Empty, string.Empty);
    }

    public void Configure(MischiefWorldEventType type, MischiefWorldEventResolveMode mode, bool disableAfterNpcResponse, bool autoComplete, float cooldown, string startSfx, string completeSfx)
    {
        eventType = type;
        resolveMode = mode;
        disableTargetAfterNpcResponse = disableAfterNpcResponse;
        autoCompleteOnStart = autoComplete;
        autoCompleteCooldown = Mathf.Max(0f, cooldown);
        startSfxId = startSfx ?? string.Empty;
        completeSfxId = completeSfx ?? string.Empty;
    }

    public void ConfigureLights(Light[] lights, Renderer[] renderers)
    {
        controlledLights = FilterValidControlledLights(lights);
        tintRenderers = renderers;
        controlLightsLocally = controlledLights != null && controlledLights.Length > 0;
        CacheLightState();
        ApplyAvailableVisualState();
    }


    public bool ValidateLightSetup(out string report)
    {
        string targetId = WorldEventTargetId;
        MischiefWorldEventType resolvedType = ResolveEventTypeFromTargetId(targetId);

        if (resolvedType != MischiefWorldEventType.LightToggle)
        {
            report = $"{name} is not a light switch world event target.";
            return true;
        }

        if (controlledLights == null || controlledLights.Length == 0)
        {
            report = $"{name} has no direct Light references. Assign OfficePointLight explicitly.";
            return false;
        }

        int validCount = 0;
        for (int i = 0; i < controlledLights.Length; i++)
        {
            Light light = controlledLights[i];
            if (light == null)
            {
                continue;
            }

            if (light.type == LightType.Directional)
            {
                report = $"{name} directly references Directional Light '{light.name}'. The switch must not control ambient/base lighting.";
                return false;
            }

            SceneLightRole role = light.GetComponent<SceneLightRole>();
            if (role != null && role.Role == SceneLightRoleType.SecuritySpotlight)
            {
                report = $"{name} directly references Security spotlight '{light.name}'. Assign OfficePointLight instead.";
                return false;
            }

            validCount++;
        }

        if (validCount == 0)
        {
            report = $"{name} has no valid direct Light references after filtering.";
            return false;
        }

        report = $"{name} uses {validCount} direct Light references. No light index/order lookup is used.";
        return true;
    }

    public bool CanStartWorldEvent()
    {
        if (eventActive)
        {
            return false;
        }

        MischiefWorldEventType resolvedType = ResolveEventTypeFromTargetId(WorldEventTargetId);
        if (resolvedType == MischiefWorldEventType.LightToggle && controlLightsLocally && lightMustBeOffToStart)
        {
            return !lightsEnabled;
        }

        return true;
    }

    public string GetUnavailableReason()
    {
        if (eventActive)
        {
            return "Event already active";
        }

        MischiefWorldEventType resolvedType = ResolveEventTypeFromTargetId(WorldEventTargetId);
        if (resolvedType == MischiefWorldEventType.LightToggle && controlLightsLocally && lightMustBeOffToStart && lightsEnabled)
        {
            return "Light is already on";
        }

        return string.Empty;
    }

    public MischiefWorldEventResult Report(MischiefContext mischiefContext)
    {
        ResolveCoreFacade();
        RegisterTargetIfPossible();

        string targetId = ResolveTargetId(mischiefContext.TargetId);
        MischiefWorldEventType resolvedType = ResolveEventTypeFromTargetId(targetId, mischiefContext.MischiefType);

        if (resolvedType == MischiefWorldEventType.None)
        {
            return MischiefWorldEventResult.Ignored(targetId, resolvedType, transform.position, "No world event route for this target.");
        }

        if (!CanStartWorldEvent())
        {
            return MischiefWorldEventResult.Ignored(targetId, resolvedType, transform.position, GetUnavailableReason());
        }

        if (coreFacade == null)
        {
            return MischiefWorldEventResult.Ignored(targetId, resolvedType, transform.position, "CoreFacade is missing.");
        }

        MischiefWorldEventResolveMode resolvedMode = resolveMode;
        if (resolvedMode == MischiefWorldEventResolveMode.None)
        {
            resolvedMode = MischiefWorldEventContext.GetDefaultResolveMode(resolvedType);
        }

        bool disableAfterResponse = disableTargetAfterNpcResponse || MischiefWorldEventContext.ShouldDisableTargetAfterResponse(resolvedType);

        MischiefWorldEventContext eventContext = new MischiefWorldEventContext(
            mischiefContext.ActorId,
            targetId,
            resolvedType,
            transform.position,
            resolvedMode,
            preferredNpcId,
            preferredNpcType,
            disableAfterResponse);

        return coreFacade.ReportMischiefWorldEvent(eventContext);
    }

    public void OnWorldEventStarted(MischiefWorldEventContext context)
    {
        eventActive = true;
        PlaySfx(startSfxId);

        if (context.EventType == MischiefWorldEventType.LightToggle && controlLightsLocally)
        {
            SetLightsEnabled(true);
        }
        else
        {
            SetRendererTint(activeEventTint);
        }

        if (autoCompleteOnStart || context.EventType == MischiefWorldEventType.MicrophoneBroadcast)
        {
            StartCoroutine(AutoCompleteAfterFrame(context.TargetId));
        }
    }

    public void OnWorldEventCompleted(bool disableTarget, float cooldownDuration)
    {
        eventActive = false;
        PlaySfx(completeSfxId);

        if (ResolveEventTypeFromTargetId(WorldEventTargetId) == MischiefWorldEventType.LightToggle && controlLightsLocally && turnLightOffOnComplete)
        {
            SetLightsEnabled(false);
        }

        if (disableTarget)
        {
            SetRendererTint(disabledTint);
            return;
        }

        ApplyAvailableVisualState();
    }

    private void ResolveCoreFacade()
    {
        if (coreFacade == null)
        {
            coreFacade = FindObjectOfType<CoreFacade>();
        }

        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
        }
    }

    private void RegisterTargetIfPossible()
    {
        ResolveCoreFacade();
        if (coreFacade == null || registered)
        {
            return;
        }

        coreFacade.RegisterMischiefWorldEventTarget(this);
        registered = true;
    }

    private string ResolveTargetId()
    {
        return ResolveTargetId(string.Empty);
    }

    private string ResolveTargetId(string fallbackTargetId)
    {
        if (!string.IsNullOrWhiteSpace(targetIdOverride))
        {
            return targetIdOverride;
        }

        if (!string.IsNullOrWhiteSpace(fallbackTargetId))
        {
            return fallbackTargetId;
        }

        IMischiefTarget target = FindMischiefTarget();
        if (target != null && !string.IsNullOrWhiteSpace(target.InteractionId))
        {
            return target.InteractionId;
        }

        return gameObject.name;
    }

    private MischiefWorldEventType ResolveEventTypeFromTargetId(string targetId)
    {
        return ResolveEventTypeFromTargetId(targetId, MischiefType.Custom);
    }

    private MischiefWorldEventType ResolveEventTypeFromTargetId(string targetId, MischiefType mischiefType)
    {
        if (eventType != MischiefWorldEventType.Auto)
        {
            return eventType;
        }

        return MischiefWorldEventContext.InferEventType(targetId, mischiefType);
    }

    private IMischiefTarget FindMischiefTarget()
    {
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IMischiefTarget target)
            {
                return target;
            }
        }

        return null;
    }

    private IEnumerator AutoCompleteAfterFrame(string targetId)
    {
        yield return null;

        if (coreFacade != null && !string.IsNullOrWhiteSpace(targetId))
        {
            coreFacade.CompleteMischiefWorldEvent(targetId, false, autoCompleteCooldown);
        }
    }

    private Light[] FilterValidControlledLights(Light[] lights)
    {
        if (lights == null || lights.Length == 0)
        {
            return null;
        }

        System.Collections.Generic.List<Light> validLights = new System.Collections.Generic.List<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            Light light = lights[i];
            if (light == null)
            {
                continue;
            }

            if (light.type == LightType.Directional)
            {
                Debug.LogWarning($"[MischiefWorldEventReporter] Ignored Directional Light on {name}. Light switches should only control explicit additional lights.", this);
                continue;
            }

            validLights.Add(light);
        }

        return validLights.ToArray();
    }

    private void PlaySfx(string sfxId)
    {
        if (string.IsNullOrWhiteSpace(sfxId))
        {
            return;
        }

        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
        }

        if (audioManager != null)
        {
            audioManager.PlaySfxAt(sfxId, transform.position);
        }
    }

    private void CacheLightState()
    {
        lightsEnabled = false;
        if (controlledLights == null)
        {
            return;
        }

        for (int i = 0; i < controlledLights.Length; i++)
        {
            if (controlledLights[i] != null && controlledLights[i].enabled)
            {
                lightsEnabled = true;
                return;
            }
        }
    }

    private void SetLightsEnabled(bool enabled)
    {
        lightsEnabled = enabled;

        if (controlledLights != null)
        {
            for (int i = 0; i < controlledLights.Length; i++)
            {
                if (controlledLights[i] != null)
                {
                    controlledLights[i].enabled = enabled;
                }
            }
        }

        SetRendererTint(enabled ? activeEventTint : availableTint);
    }

    private void ApplyAvailableVisualState()
    {
        if (ResolveEventTypeFromTargetId(WorldEventTargetId) == MischiefWorldEventType.LightToggle && controlLightsLocally)
        {
            SetRendererTint(lightsEnabled ? activeEventTint : availableTint);
            return;
        }

        SetRendererTint(availableTint);
    }

    private void SetRendererTint(Color color)
    {
        if (tintRenderers == null)
        {
            return;
        }

        for (int i = 0; i < tintRenderers.Length; i++)
        {
            if (tintRenderers[i] != null)
            {
                tintRenderers[i].material.color = color;
            }
        }
    }
}
