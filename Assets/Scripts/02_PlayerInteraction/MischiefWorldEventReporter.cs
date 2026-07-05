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
        eventType = type;
        resolveMode = mode;
        disableTargetAfterNpcResponse = disableAfterNpcResponse;
    }

    public void ConfigureLights(Light[] lights, Renderer[] renderers)
    {
        controlledLights = lights;
        tintRenderers = renderers;
        controlLightsLocally = lights != null && lights.Length > 0;
        CacheLightState();
        ApplyAvailableVisualState();
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

        if (context.EventType == MischiefWorldEventType.LightToggle && controlLightsLocally)
        {
            SetLightsEnabled(true);
            return;
        }

        SetRendererTint(activeEventTint);
    }

    public void OnWorldEventCompleted(bool disableTarget, float cooldownDuration)
    {
        eventActive = false;

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
