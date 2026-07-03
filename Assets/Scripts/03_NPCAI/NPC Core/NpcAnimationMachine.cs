using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class NpcAnimationMachine : MonoBehaviour
{
    [Header("Animations")]
    [SerializeField] private NpcAnimParams animations;

    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private NpcController controller;

    [Header("Animator Params")]
    [SerializeField] private string speedParamName = "Speed";

    [Header("Animator IK")]
    [SerializeField] private string ikActiveParamName = "IKActive";
    [SerializeField] private bool setIKActiveOnPlay = true;

    [Header("Animator Layer")]
    [SerializeField] private int baseLayerIndex = 0;

    [Header("Movement Animation")]
    [SerializeField] private float speedDampTime = 0.08f;

    [Tooltip("If true, the locomotion blend tree receives the configured NavMeshAgent speed while the agent has an active movement intent. This avoids animation speed being halved by braking, turning, or avoidance.")]
    [SerializeField] private bool useConfiguredAgentSpeedForLocomotion = true;

    [Tooltip("Extra distance beyond stoppingDistance treated as still moving for animation intent.")]
    [SerializeField] private float movementIntentDistanceBuffer = 0.05f;

    [Header("Child Animator Root Motion Relay")]
    [SerializeField] private bool relayChildAnimatorRootMotionToRoot = true;

    [SerializeField] private Transform visualRoot;

    private bool relayingRootMotion;
    private Vector3 visualRootInitialLocalPosition;
    private Quaternion visualRootInitialLocalRotation;
    private Vector3 previousVisualLocalPosition;
    private Quaternion previousVisualLocalRotation;

    private int ikActiveHash;
    private bool hasIKActiveParam;
    private bool animatorIKActive;

    private int speedHash;
    private AnimState? currentAnimState;

    public AnimState? CurrentAnimState => currentAnimState;

    private void Awake()
    {
        LazyInstantiate();

        if (!string.IsNullOrWhiteSpace(speedParamName))
            speedHash = Animator.StringToHash(speedParamName);

        CacheAnimatorIKParam();
    }

    private void Update()
    {
        UpdateLocomotionSpeed();
    }

    private void LateUpdate()
    {
        RelayChildRootMotionToNpcRoot();
    }

    private void RelayChildRootMotionToNpcRoot()
    {
        if (!relayingRootMotion)
            return;

        if (visualRoot == null)
            return;

        Vector3 localDelta = visualRoot.localPosition - previousVisualLocalPosition;

        if (localDelta.sqrMagnitude > 0f)
        {
            Vector3 worldDelta = transform.TransformVector(localDelta);

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(transform.position + worldDelta);
            }
            else
            {
                transform.position += worldDelta;
            }
        }

        // Keep the visual child anchored under the gameplay root.
        visualRoot.localPosition = visualRootInitialLocalPosition;
        visualRoot.localRotation = visualRootInitialLocalRotation;

        previousVisualLocalPosition = visualRoot.localPosition;
        previousVisualLocalRotation = visualRoot.localRotation;
    }

    public void Play(AnimState animState, bool forceReplay = false)
    {
        LazyInstantiate();

        if (animator == null)
        {
            Debug.LogWarning($"[NPC Anim] {name}: Animator missing.");
            return;
        }

        if (!TryResolveAnimState(animState, out AnimParam param, out int shortHash, out int fullHash))
            return;

        if (!forceReplay && currentAnimState.HasValue && currentAnimState.Value == animState)
            return;

        int stateHash;

        if (animator.HasState(baseLayerIndex, fullHash))
        {
            stateHash = fullHash;
        }
        else if (animator.HasState(baseLayerIndex, shortHash))
        {
            stateHash = shortHash;
        }
        else
        {
            Debug.LogWarning(
                $"[NPC Anim] {name}: Animator does not have state '{param.animatorStateName}' " +
                $"or 'Base Layer.{param.animatorStateName}' on layer {baseLayerIndex}."
            );
            return;
        }

        ApplyRootMotionSetting(param);

        if (setIKActiveOnPlay)
            SetAnimatorIK(param.enableAnimatorIK);

        animator.CrossFadeInFixedTime(
            stateHash,
            param.crossFadeTime,
            baseLayerIndex
        );

        currentAnimState = animState;

        Debug.Log($"[NPC Anim] {name}: Play {animState} -> {param.animatorStateName}");
    }

    private bool TryResolveAnimState(
        AnimState animState,
        out AnimParam param,
        out int shortHash,
        out int fullHash
    )
    {
        LazyInstantiate();

        param = null;
        shortHash = 0;
        fullHash = 0;

        if (animations == null)
        {
            Debug.LogWarning($"[NPC Anim] {name}: NpcAnimParams missing.");
            return false;
        }

        if (!animations.TryGetAnimParam(animState, out param))
        {
            Debug.LogWarning($"[NPC Anim] {name}: No mapping for {animState}.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(param.animatorStateName))
        {
            Debug.LogWarning($"[NPC Anim] {name}: Animator state name is empty for {animState}.");
            return false;
        }

        shortHash = Animator.StringToHash(param.animatorStateName);
        fullHash = Animator.StringToHash($"Base Layer.{param.animatorStateName}");

        return true;
    }

    private void ApplyRootMotionSetting(AnimParam param)
    {
        if (animator == null || param == null)
            return;

        bool shouldApplyRootMotion = param.applyRootMotion;

        if (animator.applyRootMotion != shouldApplyRootMotion)
        {
            animator.applyRootMotion = shouldApplyRootMotion;

            Debug.Log(
                $"[NPC Anim] {name}: applyRootMotion = {shouldApplyRootMotion} " +
                $"for {param.animState}"
            );
        }

        ApplyAgentRootMotionPolicy(param);
        ApplyRootMotionRelayPolicy(param);
    }

    private void ApplyRootMotionRelayPolicy(AnimParam param)
    {
        bool shouldRelay =
            relayChildAnimatorRootMotionToRoot &&
            param.applyRootMotion &&
            animator != null &&
            visualRoot != null &&
            visualRoot != transform;

        if (shouldRelay)
        {
            BeginChildRootMotionRelay();
        }
        else
        {
            EndChildRootMotionRelay();
        }
    }

    private void BeginChildRootMotionRelay()
    {
        if (relayingRootMotion)
            return;

        relayingRootMotion = true;

        visualRootInitialLocalPosition = visualRoot.localPosition;
        visualRootInitialLocalRotation = visualRoot.localRotation;

        previousVisualLocalPosition = visualRoot.localPosition;
        previousVisualLocalRotation = visualRoot.localRotation;

        Debug.Log($"[NPC Anim] {name}: Begin child root-motion relay.");
    }

    private void EndChildRootMotionRelay()
    {
        if (!relayingRootMotion)
            return;

        relayingRootMotion = false;

        if (visualRoot != null)
        {
            visualRoot.localPosition = visualRootInitialLocalPosition;
            visualRoot.localRotation = visualRootInitialLocalRotation;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.nextPosition = transform.position;

        Debug.Log($"[NPC Anim] {name}: End child root-motion relay.");
    }

    private void ApplyAgentRootMotionPolicy(AnimParam param)
    {
        if (agent == null)
            return;

        bool shouldPauseAgentUpdates =
            param.applyRootMotion &&
            param.pauseAgentUpdateWhileRootMotion;

        agent.updatePosition = !shouldPauseAgentUpdates;
        agent.updateRotation = !shouldPauseAgentUpdates;

        if (!shouldPauseAgentUpdates && agent.enabled && agent.isOnNavMesh)
        {
            agent.nextPosition = transform.position;
        }

        Debug.Log(
            $"[NPC Anim] {name}: agent.updatePosition = {agent.updatePosition}, " +
            $"agent.updateRotation = {agent.updateRotation}"
        );
    }

    public void PlayLocomotion()
    {
        Play(AnimState.Locomotion);
    }

    public void PlaySearch()
    {
        Play(AnimState.Search);
    }

    public void PlaySearchToIdle()
    {
        Play(AnimState.TransitionSearchToIdle, true);
    }

    public void PlayDive()
    {
        Play(AnimState.Dive, true);
    }

    public void PlayDiveToCooldown()
    {
        Play(AnimState.TransitionDiveToCooldown, true);
    }

    public void PlayChaseCooldown()
    {
        Play(AnimState.ChaseCooldown, true);
    }

    public void PlayDiveCooldown()
    {
        Play(AnimState.DiveCooldown, true);
    }

    public void PlayCooldownFrom(NpcState sourceState)
    {
        switch (sourceState)
        {
            case NpcState.Dive:
                PlayDiveCooldown();
                break;

            case NpcState.Chase:
                PlayChaseCooldown();
                break;

            default:
                PlayChaseCooldown();
                break;
        }
    }

    public void PlayCaught()
    {
        Play(AnimState.Caught, true);
    }

    public void PlayOverrideMove()
    {
        Play(AnimState.OverrideMove);
    }

    public void PlayStandToSit()
    {
        Play(AnimState.TransitionStandToSit, true);
    }

    public void PlayIdleSitting()
    {
        Play(AnimState.IdleSitting);
    }

    public void PlayByNpcState(NpcState state)
    {
        switch (state)
        {
            case NpcState.Idle:
            case NpcState.Chase:
                PlayLocomotion();
                break;

            case NpcState.Search:
                PlaySearch();
                break;

            case NpcState.Cooldown:
                PlayChaseCooldown(); // fallback only; prefer PlayCooldownFrom(previousState).
                break;

            case NpcState.Override:
                PlayOverrideMove();
                break;

            case NpcState.Dive:
                PlayDive();
                break;
        }
    }

    public void ResetToLocomotion()
    {
        currentAnimState = null;
        PlayLocomotion();
    }

    private void UpdateLocomotionSpeed()
    {
        if (animator == null || string.IsNullOrWhiteSpace(speedParamName))
            return;

        float speed = 0f;

        if (agent != null && agent.enabled)
        {
            if (useConfiguredAgentSpeedForLocomotion && HasMovementIntent())
            {
                speed = agent.speed;
            }
            else
            {
                speed = agent.velocity.magnitude;
            }
        }

        animator.SetFloat(speedHash, speed, speedDampTime, Time.deltaTime);
    }

    private bool HasMovementIntent()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        if (agent.isStopped || agent.pathPending || !agent.hasPath)
            return false;

        if (float.IsInfinity(agent.remainingDistance))
            return agent.desiredVelocity.sqrMagnitude > 0.01f;

        return
            agent.remainingDistance > agent.stoppingDistance + movementIntentDistanceBuffer ||
            agent.desiredVelocity.sqrMagnitude > 0.01f;
    }

    public Coroutine PlayAndNotifyWhenFinished(
        AnimState animState,
        Action onFinished,
        bool forceReplay = true,
        float normalizedExitTime = 0.98f,
        float timeout = 5f
    )
    {
        Play(animState, forceReplay);

        return StartCoroutine(
            WaitForStateFinished(
                animState,
                success =>
                {
                    if (!success)
                    {
                        Debug.LogWarning(
                            $"[NPC Anim] {name}: Timed out while waiting for {animState} to finish."
                        );
                    }

                    onFinished?.Invoke();
                },
                normalizedExitTime,
                timeout
            )
        );
    }

    public IEnumerator WaitForStateEntered(
        AnimState animState,
        Action<bool> onFinished = null,
        float timeout = 5f
    )
    {
        if (!TryResolveAnimState(animState, out _, out int shortHash, out int fullHash))
        {
            onFinished?.Invoke(false);
            yield break;
        }

        float elapsed = 0f;

        while (timeout <= 0f || elapsed < timeout)
        {
            if (IsCurrentState(shortHash, fullHash) || IsNextState(shortHash, fullHash))
            {
                onFinished?.Invoke(true);
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"[NPC Anim] {name}: Timeout waiting for {animState} to enter.");
        onFinished?.Invoke(false);
    }

    public IEnumerator WaitForStateFinished(
        AnimState animState,
        Action<bool> onFinished = null,
        float normalizedExitTime = 0.98f,
        float timeout = 5f
    )
    {
        if (!TryResolveAnimState(animState, out _, out int shortHash, out int fullHash))
        {
            onFinished?.Invoke(false);
            yield break;
        }

        float elapsed = 0f;

        while (timeout <= 0f || elapsed < timeout)
        {
            if (IsCurrentState(shortHash, fullHash))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!IsCurrentState(shortHash, fullHash))
        {
            Debug.LogWarning($"[NPC Anim] {name}: Timeout waiting for {animState} to become current.");
            onFinished?.Invoke(false);
            yield break;
        }

        while (timeout <= 0f || elapsed < timeout)
        {
            AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(baseLayerIndex);

            bool isStillCurrentState =
                info.shortNameHash == shortHash ||
                info.fullPathHash == fullHash;

            if (!isStillCurrentState)
            {
                onFinished?.Invoke(true);
                yield break;
            }

            if (!animator.IsInTransition(baseLayerIndex) &&
                info.normalizedTime >= normalizedExitTime)
            {
                onFinished?.Invoke(true);
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Debug.LogWarning($"[NPC Anim] {name}: Timeout waiting for {animState} to finish.");
        onFinished?.Invoke(false);
    }

    private bool IsCurrentState(int shortHash, int fullHash)
    {
        if (animator == null)
            return false;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(baseLayerIndex);

        return
            info.shortNameHash == shortHash ||
            info.fullPathHash == fullHash;
    }

    private bool IsNextState(int shortHash, int fullHash)
    {
        if (animator == null || !animator.IsInTransition(baseLayerIndex))
            return false;

        AnimatorStateInfo info = animator.GetNextAnimatorStateInfo(baseLayerIndex);

        return
            info.shortNameHash == shortHash ||
            info.fullPathHash == fullHash;
    }

    public bool AnimatorIKActive => animatorIKActive;

    public void SetAnimatorIK(bool value)
    {
        LazyInstantiate();

        animatorIKActive = value;

        if (animator == null)
            return;

        if (!hasIKActiveParam)
            CacheAnimatorIKParam();

        if (!hasIKActiveParam)
            return;

        animator.SetBool(ikActiveHash, value);
    }

    private void CacheAnimatorIKParam()
    {
        hasIKActiveParam = false;

        if (animator == null)
            return;

        if (string.IsNullOrWhiteSpace(ikActiveParamName))
            return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.name == ikActiveParamName &&
                param.type == AnimatorControllerParameterType.Bool)
            {
                ikActiveHash = Animator.StringToHash(ikActiveParamName);
                hasIKActiveParam = true;
                return;
            }
        }

        Debug.LogWarning(
            $"[NPC Anim] {name}: Animator bool parameter '{ikActiveParamName}' is missing. " +
            "SetAnimatorIK will update the local flag, but cannot write to Animator."
        );
    }

    private void LazyInstantiate()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (visualRoot == null && animator != null)
            visualRoot = animator.transform;

        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (controller == null)
            controller = GetComponent<NpcController>();
    }
}
