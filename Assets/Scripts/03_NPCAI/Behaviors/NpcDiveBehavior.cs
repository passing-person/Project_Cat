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

    private bool diveCatchWindowOpen;
    private bool PlayerInCatchRange => view != null && view.PlayerInCatchRange;

    private NpcNavigate nav;
    private NpcController controller;
    private NpcView view;
    private NpcAnimationMachine anim;

    private PlayerController player;

    private Coroutine diveSequenceRoutine;
    private bool diveStarted;

    private bool PlayerInReach => view != null && view.PlayerInReach;

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
    public void Cleaner() => StartDive();
    public void Security() => StartDive();

    public void ExitState()
    {
        CleanupDive();
    }

    private void StartDive()
    {
        LazyInstantiate();

        if (diveStarted)
            return;

        diveStarted = true;
        diveCatchWindowOpen = false;

        nav.StopPatrol();
        nav.StopNav();

        if (diveSequenceRoutine != null)
            StopCoroutine(diveSequenceRoutine);

        diveSequenceRoutine = StartCoroutine(DiveSequenceRoutine());
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

    private IEnumerator DiveSequenceRoutine()
    {
        yield return FacePlayerBeforeDive();

        if (!IsValidDiveState())
        {
            diveSequenceRoutine = null;
            yield break;
        }

        anim.PlayDive();

        yield return WaitForDiveSequenceFinished();
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
    }
}
