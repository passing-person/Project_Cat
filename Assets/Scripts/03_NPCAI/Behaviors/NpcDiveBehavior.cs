using System.Collections;
using UnityEngine;

public class NpcDiveBehavior : MonoBehaviour
{
    [Header("Animation Completion")]
    [SerializeField] private float animationStateWaitTimeout = 6f;
    [SerializeField, Range(0f, 1f)] private float transitionFinishNormalizedTime = 0.98f;

    [Header("Dive Facing")]
    [SerializeField] private bool facePlayerBeforeDive = true;

    [SerializeField, Min(0f)]
    private float preDiveTurnSpeedDeg = 720f;

    [SerializeField, Min(0f)]
    private float maxPreDiveTurnTime = 0.25f;

    [SerializeField, Range(0f, 30f)]
    private float facePlayerAngleTolerance = 3f;

    [SerializeField]
    private bool snapToPlayerDirectionAfterMaxTurnTime = true;

    [Tooltip("If true, the NPC is snapped exactly to the current flat player direction immediately before PlayDive. This makes the dive direction deterministic instead of allowing angle-tolerance drift.")]
    [SerializeField] private bool snapExactlyToPlayerDirectionBeforeDive = true;

    [Header("Cleaner Double Dive")]
    [Tooltip("If true, Cleaner NPCs perform two Dive plays inside one Dive state before entering cooldown.")]
    [SerializeField] private bool cleanerUsesDoubleDive = true;

    [Tooltip("Number of consecutive dives for Cleaner. Keep this at 2 for the requested Cleaner double-dive behavior.")]
    [SerializeField, Min(1)] private int cleanerDiveCount = 2;

    [Tooltip("The first dive can be replayed when the Dive state reaches this normalized time or exits. This bypasses DiveCooldown between dives.")]
    [SerializeField, Range(0f, 1f)] private float replayNextDiveNormalizedTime = 0.98f;

    [Tooltip("Optional delay between consecutive Cleaner dives. Leave at 0 for immediate double dive.")]
    [SerializeField, Min(0f)] private float delayBetweenCleanerDives = 0f;


    private bool diveCatchWindowOpen;
    private bool PlayerInCatchRange => view != null && view.PlayerInCatchRange;

    private NpcNavigate nav;
    private NpcController controller;
    private NpcView view;
    private NpcAnimationMachine anim;
    private Animator animator;

    private PlayerController player;

    private Coroutine diveSequenceRoutine;
    private bool diveStarted;


    private void Awake()
    {
        LazyInstantiate();
    }

    private void OnEnable()
    {
        LazyInstantiate();

        if (view != null)
            view.PlayerInCatchRangeFlagChange += ResolvePlayerCatchRangeChanged;
    }

    private void OnDisable()
    {
        if (view != null)
            view.PlayerInCatchRangeFlagChange -= ResolvePlayerCatchRangeChanged;
    }

    public void Supervisor() => StartDive();
    public void Worker() => StartDive();
    public void Cleaner() 
    {
        if (cleanerUsesDoubleDive)
            StartDiveSequence(cleanerDiveCount);
        else StartDive();
    } 
    public void Security() => StartDive();

    public void ExitState()
    {
        CleanupDive();
    }

    private void StartDive()
    {
        StartDiveSequence(1);
    }

    private void StartDiveSequence(int diveCount)
    {
        LazyInstantiate();

        if (diveStarted)
            return;

        diveStarted = true;
        diveCatchWindowOpen = false;

        if (nav != null)
        {
            nav.StopPatrol();
            nav.StopNav();
        }

        if (diveSequenceRoutine != null)
            StopCoroutine(diveSequenceRoutine);

        diveSequenceRoutine = StartCoroutine(DiveSequenceRoutine(Mathf.Max(1, diveCount)));
    }

    private IEnumerator WaitForDiveSequenceFinished()
    {
        bool enteredTransition = false;

        yield return anim.WaitForStateEntered(
            AnimState.TransitionDiveToCooldown,
            success => enteredTransition = success,
            animationStateWaitTimeout
        );

        if (!IsValidDiveState())
        {
            diveSequenceRoutine = null;
            yield break;
        }

        if (!enteredTransition)
        {
            Debug.LogWarning(
                $"[NPC Dive] {name}: Animator never entered TransitionDiveToCooldown. " +
                "Forcing Dive to finish so gameplay does not get stuck."
            );

            diveSequenceRoutine = null;
            ResolveDiveAnimationFinished();
            yield break;
        }

        bool finishedTransition = false;

        yield return anim.WaitForStateFinished(
            AnimState.TransitionDiveToCooldown,
            success => finishedTransition = success,
            transitionFinishNormalizedTime,
            animationStateWaitTimeout
        );

        if (!IsValidDiveState())
        {
            diveSequenceRoutine = null;
            yield break;
        }

        if (!finishedTransition)
        {
            Debug.LogWarning(
                $"[NPC Dive] {name}: TransitionDiveToCooldown did not finish before timeout. " +
                "Forcing Dive to finish so gameplay does not get stuck."
            );
        }

        diveSequenceRoutine = null;
        ResolveDiveAnimationFinished();
    }

    private IEnumerator WaitForDiveReplayPoint(int completedDiveIndex, int totalDiveCount)
    {
        bool finishedDive = false;

        yield return anim.WaitForStateFinished(
            AnimState.Dive,
            success => finishedDive = success,
            replayNextDiveNormalizedTime,
            animationStateWaitTimeout
        );

        if (!IsValidDiveState())
        {
            diveSequenceRoutine = null;
            yield break;
        }

        if (!finishedDive)
        {
            Debug.LogWarning(
                $"[NPC Dive] {name}: Cleaner dive {completedDiveIndex}/{totalDiveCount} " +
                "did not reach the replay point before timeout. Replaying the next dive anyway."
            );
        }
    }

    private bool TryResolvePlayerCaughtImmediately()
    {
        if (!IsValidDiveState())
            return false;

        if (!diveCatchWindowOpen)
            return false;

        if (!PlayerInCatchRange)
            return false;

        ResolvePlayerCaught();
        return true;
    }

    private void ResolvePlayerCatchRangeChanged()
    {
        if (!IsValidDiveState())
            return;

        if (!diveCatchWindowOpen)
            return;

        if (!PlayerInCatchRange)
            return;

        ResolvePlayerCaught();
    }

    private void ResolvePlayerCaught()
    {
        if (!IsValidDiveState())
            return;

        CleanupDive();

        if (anim != null)
            anim.PlayCaught();

        if (controller != null)
            controller.OnPlayerCaught(player);
    }

    public void ResolveDiveAnimationFinished()
    {
        if (!IsValidDiveState())
            return;

        CleanupDive();

        controller.CurrentNpcState = NpcState.Cooldown;
    }

    private bool IsValidDiveState()
    {
        LazyInstantiate();

        return
            diveStarted &&
            controller != null &&
            controller.CurrentNpcState == NpcState.Dive;
    }

    private void CleanupDive()
    {
        diveStarted = false;
        diveCatchWindowOpen = false;

        if (diveSequenceRoutine != null)
        {
            StopCoroutine(diveSequenceRoutine);
            diveSequenceRoutine = null;
        }

        if (nav != null)
            nav.StopNav();
    }

    private IEnumerator FacePlayerBeforeDive()
    {
        if (!facePlayerBeforeDive)
            yield break;

        if (player == null)
            yield break;

        Vector3 flatDirectionToPlayer = player.transform.position - transform.position;
        flatDirectionToPlayer.y = 0f;

        if (flatDirectionToPlayer.sqrMagnitude <= 0.0001f)
            yield break;

        Quaternion targetRotation = Quaternion.LookRotation(
            flatDirectionToPlayer.normalized,
            Vector3.up
        );

        float elapsed = 0f;

        while (elapsed < maxPreDiveTurnTime)
        {
            if (!IsValidDiveState())
                yield break;

            float angle = Quaternion.Angle(transform.rotation, targetRotation);

            if (angle <= facePlayerAngleTolerance)
                yield break;

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                preDiveTurnSpeedDeg * Time.deltaTime
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (snapToPlayerDirectionAfterMaxTurnTime)
        {
            Vector3 finalDirection = player.transform.position - transform.position;
            finalDirection.y = 0f;

            if (finalDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(
                    finalDirection.normalized,
                    Vector3.up
                );
            }
        }
    }

    private void SnapExactlyToPlayerDirectionBeforeDive()
    {
        if (!snapExactlyToPlayerDirectionBeforeDive)
            return;

        if (!facePlayerBeforeDive)
            return;

        if (player == null)
            return;

        Vector3 flatDirectionToPlayer = player.transform.position - transform.position;
        flatDirectionToPlayer.y = 0f;

        if (flatDirectionToPlayer.sqrMagnitude <= 0.0001f)
            return;

        transform.rotation = Quaternion.LookRotation(
            flatDirectionToPlayer.normalized,
            Vector3.up
        );
    }

    private IEnumerator DiveSequenceRoutine(int diveCount)
    {
        for (int diveIndex = 1; diveIndex <= diveCount; diveIndex++)
        {
            // Re-aim before every individual dive. This is what allows Cleaner to
            // change direction between the first and second dive without leaving
            // the gameplay Dive state.
            diveCatchWindowOpen = false;

            yield return FacePlayerBeforeDive();

            if (!IsValidDiveState())
            {
                diveSequenceRoutine = null;
                yield break;
            }

            SnapExactlyToPlayerDirectionBeforeDive();

            // PlayDive(forceReplay=true) is used inside NpcAnimationMachine, so this
            // can replay Dive even if the Animator has already entered or is blending
            // toward TransitionDiveToCooldown after the previous dive.
            anim.PlayDive();

            diveCatchWindowOpen = true;

            // Do not rely only on the flag-change event. If the player is already
            // inside catch range when the window opens, catch immediately.
            if (TryResolvePlayerCaughtImmediately())
                yield break;

            bool isFinalDive = diveIndex >= diveCount;

            if (isFinalDive)
            {
                // Only the final dive is allowed to finish the normal Dive ->
                // TransitionDiveToCooldown -> gameplay Cooldown path.
                yield return WaitForDiveSequenceFinished();
                yield break;
            }

            // For intermediate Cleaner dives, wait only until Dive reaches its
            // replay point. Do not call ResolveDiveAnimationFinished and do not
            // switch controller.CurrentNpcState to Cooldown here.
            yield return WaitForDiveReplayPoint(diveIndex, diveCount);

            if (!IsValidDiveState())
            {
                diveSequenceRoutine = null;
                yield break;
            }

            diveCatchWindowOpen = false;

            if (delayBetweenCleanerDives > 0f)
                yield return new WaitForSeconds(delayBetweenCleanerDives);
        }
    }

    private void LazyInstantiate()
    {
        if (nav == null)
            nav = GetComponent<NpcNavigate>();

        if (controller == null)
            controller = GetComponent<NpcController>();

        if (view == null)
            view = GetComponent<NpcView>();

        if (anim == null)
            anim = GetComponent<NpcAnimationMachine>();

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }
}
