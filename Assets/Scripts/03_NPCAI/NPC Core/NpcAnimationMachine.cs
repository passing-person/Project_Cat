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

    [Header("Code Driven Horizontal Motion")]
    [SerializeField] private bool enableCodeDrivenHorizontalMotion = true;

    [Tooltip("If true, code-driven horizontal motion uses absolute placement from a precomputed start/target instead of accumulating per-frame deltas. This makes the final horizontal distance deterministic.")]
    [SerializeField] private bool deterministicCodeDrivenHorizontalMotion = true;

    [Tooltip("If true, Animator root motion, child root-motion relay, and NavMeshAgent transform updates are suppressed while code-driven horizontal motion is controlling the NPC root.")]
    [SerializeField] private bool isolateCodeDrivenHorizontalMotion = true;

    [Tooltip("If true, completed code-driven horizontal motion is snapped exactly onto its precomputed target point.")]
    [SerializeField] private bool snapCodeDrivenMotionToTargetOnComplete = true;

    [Header("Close Range Dive Warp")]
    [Tooltip("If true, a Dive started while the player is inside the close-range threshold will warp directly to the player position instead of using code-driven horizontal shift.")]
    [SerializeField] private bool enableCloseRangeDiveWarp = true;

    [Tooltip("If the player is closer than this horizontal distance when Dive starts, the NPC warps to the player position and skips horizontal shift. NpcView can use this as its close-range trigger threshold.")]
    [SerializeField, Min(0f)] private float closeRangeDiveDistanceThreshold = 0.75f;

    private bool codeMotionActive;
    private AnimParam codeMotionParam;
    private AnimState codeMotionAnimState;
    private Vector3 codeMotionDirection;
    private Vector3 codeMotionStartPosition;
    private Vector3 codeMotionTargetPosition;
    private float codeMotionElapsed;
    private float codeMotionPreviousCurveValue;

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

    private AnimParam currentAnimParam;
    private NpcView view;

    public AnimState? CurrentAnimState => currentAnimState;
    public NpcAnchorMode CurrentNpcAnchorMode => currentAnimParam.popoutAnchorMode;
    public float CloseRangeDiveDistanceThreshold => closeRangeDiveDistanceThreshold;

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
        UpdateCodeDrivenHorizontalMotion();
    }

    private void LateUpdate()
    {
        RelayChildRootMotionToNpcRoot();
    }

    private void RelayChildRootMotionToNpcRoot()
    {
        if (!relayingRootMotion)
            return;

        if (codeMotionActive && isolateCodeDrivenHorizontalMotion)
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

        StopCodeDrivenHorizontalMotion(false);

        currentAnimParam = param;

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

        ConfigureCodeDrivenHorizontalMotion(animState, param);

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

    private void ConfigureCodeDrivenHorizontalMotion(AnimState animState, AnimParam param)
    {
        if (TryApplyCloseRangeDiveWarp(animState))
            return;

        if (!enableCodeDrivenHorizontalMotion)
            return;

        if (param == null)
            return;

        if (!param.useCodeDrivenHorizontalMotion)
            return;

        if (param.horizontalMotionDistance <= 0f)
            return;

        codeMotionActive = true;
        codeMotionParam = param;
        codeMotionAnimState = animState;
        codeMotionElapsed = 0f;
        codeMotionPreviousCurveValue = 0f;
        codeMotionStartPosition = transform.position;

        codeMotionDirection = transform.forward;
        codeMotionDirection.y = 0f;

        if (codeMotionDirection.sqrMagnitude <= 0.0001f)
            codeMotionDirection = Vector3.forward;

        codeMotionDirection.Normalize();
        codeMotionTargetPosition = codeMotionStartPosition + codeMotionDirection * param.horizontalMotionDistance;

        if (deterministicCodeDrivenHorizontalMotion && isolateCodeDrivenHorizontalMotion)
        {
            // Gameplay root is now controlled only by this script.
            // Animation remains visual; root-motion relay cannot add extra distance.
            if (animator != null)
                animator.applyRootMotion = false;

            EndChildRootMotionRelay();

            if (agent != null && agent.enabled && agent.isOnNavMesh)
            {
                agent.updatePosition = false;
                agent.updateRotation = false;
                agent.nextPosition = transform.position;
            }
        }

        Debug.Log(
            $"[NPC Anim] {name}: Code motion started. " +
            $"State={animState}, Distance={param.horizontalMotionDistance}, " +
            $"Duration={param.horizontalMotionDuration}, Direction={codeMotionDirection}, " +
            $"Target={codeMotionTargetPosition}"
        );
    }

    private bool TryApplyCloseRangeDiveWarp(AnimState animState)
    {
        if (animState != AnimState.Dive)
            return false;

        if (!enableCloseRangeDiveWarp)
            return false;

        if (!TryGetCloseRangeDiveWarpTarget(out Vector3 warpTarget))
            return false;

        // This dive is already close enough to connect. Do not apply the designed
        // horizontal-shift distance, and suppress visual root-motion relay so the
        // gameplay root lands exactly on the sampled player position.
        if (animator != null)
            animator.applyRootMotion = false;

        EndChildRootMotionRelay();

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.updatePosition = false;
            agent.updateRotation = false;
            agent.nextPosition = transform.position;
        }

        warpTarget.y = transform.position.y;
        WarpNpcRootPosition(warpTarget);

        Debug.Log(
            $"[NPC Anim] {name}: Close-range dive warp. " +
            $"Threshold={closeRangeDiveDistanceThreshold:0.000}, Target={warpTarget}"
        );

        return true;
    }

    private bool TryGetCloseRangeDiveWarpTarget(out Vector3 target)
    {
        LazyInstantiate();

        target = transform.position;

        if (view != null && view.TryGetCloseRangeDiveWarpTarget(out target))
            return true;

        if (view == null)
            return false;

        if (!view.TryGetPlayerPositionSnapshot(out Vector3 playerPosition))
            return false;

        Vector3 toPlayer = playerPosition - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude > closeRangeDiveDistanceThreshold * closeRangeDiveDistanceThreshold)
            return false;

        target = playerPosition;
        return true;
    }

    private void WarpNpcRootPosition(Vector3 position)
    {
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            bool warped = agent.Warp(position);

            if (!warped)
                transform.position = position;

            agent.nextPosition = transform.position;
            return;
        }

        transform.position = position;
    }

    private void UpdateCodeDrivenHorizontalMotion()
    {
        if (!codeMotionActive)
            return;

        if (codeMotionParam == null)
        {
            StopCodeDrivenHorizontalMotion(false);
            return;
        }

        codeMotionElapsed += Time.deltaTime;

        float duration = Mathf.Max(0.01f, codeMotionParam.horizontalMotionDuration);
        float progress = Mathf.Clamp01(codeMotionElapsed / duration);

        float curveValue = progress;

        if (codeMotionParam.horizontalMotionCurve != null)
            curveValue = codeMotionParam.horizontalMotionCurve.Evaluate(progress);

        curveValue = Mathf.Clamp01(curveValue);

        if (deterministicCodeDrivenHorizontalMotion)
        {
            ApplyCodeDrivenHorizontalCurveValue(curveValue);
        }
        else
        {
            float deltaCurve = curveValue - codeMotionPreviousCurveValue;

            if (deltaCurve > 0f)
            {
                float deltaDistance = deltaCurve * codeMotionParam.horizontalMotionDistance;
                Vector3 delta = codeMotionDirection * deltaDistance;

                ApplyCodeDrivenHorizontalDelta(delta);
            }
        }

        codeMotionPreviousCurveValue = curveValue;

        if (progress >= 1f)
        {
            if (deterministicCodeDrivenHorizontalMotion && snapCodeDrivenMotionToTargetOnComplete)
                ApplyCodeDrivenHorizontalCurveValue(1f);

            StopCodeDrivenHorizontalMotion(true);
        }
    }

    private void ApplyCodeDrivenHorizontalCurveValue(float curveValue)
    {
        if (codeMotionParam == null)
            return;

        curveValue = Mathf.Clamp01(curveValue);

        Vector3 target = Vector3.LerpUnclamped(
            codeMotionStartPosition,
            codeMotionTargetPosition,
            curveValue
        );

        // This motion is intentionally horizontal. Preserve the current Y so gravity,
        // slopes, or other vertical systems are not overwritten.
        target.y = transform.position.y;

        SetNpcRootPosition(target);
    }

    private void SetNpcRootPosition(Vector3 position)
    {
        if (deterministicCodeDrivenHorizontalMotion && isolateCodeDrivenHorizontalMotion)
        {
            // Do not ask the NavMeshAgent to solve or project this movement.
            // The deterministic dive target is the gameplay authority.
            transform.position = position;

            if (agent != null && agent.enabled && agent.isOnNavMesh)
                agent.nextPosition = transform.position;

            return;
        }

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            bool warped = agent.Warp(position);

            if (!warped)
                transform.position = position;

            agent.nextPosition = transform.position;
        }
        else
        {
            transform.position = position;
        }
    }

    private void ApplyCodeDrivenHorizontalDelta(Vector3 delta)
    {
        if (delta.sqrMagnitude <= 0.000001f)
            return;

        // During Dive, NpcDiveBehavior calls nav.StopNav(),
        // so the NavMeshAgent is usually disabled here.
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            bool warped = agent.Warp(transform.position + delta);

            if (!warped)
            {
                transform.position += delta;
            }
        }
        else
        {
            transform.position += delta;
        }
    }

    private void StopCodeDrivenHorizontalMotion(bool completedNormally = false)
    {
        if (!codeMotionActive)
            return;

        if (completedNormally && deterministicCodeDrivenHorizontalMotion && snapCodeDrivenMotionToTargetOnComplete)
            ApplyCodeDrivenHorizontalCurveValue(1f);

        Vector3 startFlat = codeMotionStartPosition;
        Vector3 currentFlat = transform.position;
        startFlat.y = 0f;
        currentFlat.y = 0f;

        float actualHorizontalDistance = Vector3.Distance(startFlat, currentFlat);
        float designedDistance = codeMotionParam != null ? codeMotionParam.horizontalMotionDistance : 0f;

        Debug.Log(
            $"[NPC Anim] {name}: Code motion stopped. " +
            $"State={codeMotionAnimState}, " +
            $"Completed={completedNormally}, " +
            $"Designed={designedDistance:0.000}, " +
            $"ActualHorizontal={actualHorizontalDistance:0.000}, " +
            $"FinalCurve={codeMotionPreviousCurveValue:0.000}, " +
            $"Target={codeMotionTargetPosition}"
        );

        codeMotionActive = false;
        codeMotionParam = null;
        codeMotionElapsed = 0f;
        codeMotionPreviousCurveValue = 0f;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
            agent.nextPosition = transform.position;
    }

    private void ApplyCodeMotionDelta(Vector3 delta)
    {
        if (delta.sqrMagnitude <= 0.000001f)
            return;

        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(transform.position + delta);
        }
        else
        {
            transform.position += delta;
        }
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

    public bool TryGetCurrentPopoutAnchorMode(out NpcAnchorMode mode)
    {
        mode = NpcAnchorMode.None;

        if (currentAnimParam == null)
            return false;

        if (!currentAnimParam.allowPopout)
            return false;

        mode = currentAnimParam.popoutAnchorMode;
        return mode != NpcAnchorMode.None;
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

    public void PlayFallback()
    {
        Debug.LogWarning($"[NPC Anim] {name}: cannot resolve animation clip, play fallback clip.");
        PlayCaught(); // this uses t-pose clip.
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

    public bool TryGetDiveDistance(out float diveDistance)
    {
        if (animations.TryGetAnimParam(AnimState.Dive, out AnimParam param))
        {
            diveDistance = param.horizontalMotionDistance;
            return true;
        }
        else
        {
            diveDistance = 0f;
            return false;
        }
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

        if (view == null)
            view = GetComponent<NpcView>();
    }
}
