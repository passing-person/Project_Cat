using UnityEngine;

public class NpcSearchBehavior : MonoBehaviour
{
    private NpcView view;
    private NpcTimer timer;
    private NpcController controller;
    private NpcNavigate nav;
    private NpcChaseBehavior chaseBehavior;

    private NpcAnimationMachine anim;
    private Animator animator;

    private bool PlayerInActualView => view != null && view.PlayerInActualView;
    private bool PlayerInReach => view != null && view.PlayerInReach;

    private bool searchStarted;

    private void Awake()
    {
        LazyInstantiate();
    }

    private void OnEnable()
    {
        LazyInstantiate();

        if (view != null)
        {
            view.PlayerActualViewFlagChange += ResolvePlayerActualViewChanged;
            view.PlayerInReachFlagChange += ResolvePlayerInReachChanged;
        }
    }

    private void OnDisable()
    {
        if (view != null)
        {
            view.PlayerActualViewFlagChange -= ResolvePlayerActualViewChanged;
            view.PlayerInReachFlagChange -= ResolvePlayerInReachChanged;
        }
    }

    public void Supervisor()
    {
        StartSearch();
    }

    public void Worker()
    {
        StartSearch();
    }

    public void Cleaner()
    {
        StartSearch();
    }

    public void Security()
    {
        StartSearch();
    }

    public void ExitState()
    {
        searchStarted = false;

        if (timer != null)
        {
            timer.StopTimer(NpcTimerType.Search);
            timer.ResetTimer(NpcTimerType.Search);
        }

        if (anim != null)
            anim.PlayLocomotion();
    }

    private void StartSearch()
    {
        LazyInstantiate();

        if (searchStarted)
            return;

        searchStarted = true;

        if (nav != null)
        {
            nav.StopPatrol();
            nav.StopNav();
        }

        if (anim != null)
            anim.PlaySearch();

        if (PlayerInReach)
        {
            ResolvePlayerFound();
            return;
        }

        if (PlayerInActualView)
        {
            ResolvePlayerFound();
            return;
        }

        if (timer != null)
            timer.StartTimer(NpcTimerType.Search, ResolveSearchTimeOver);
    }

    private void ResolvePlayerActualViewChanged()
    {
        if (!IsValidSearchState())
            return;

        if (!PlayerInActualView)
            return;

        ResolvePlayerFound();
    }

    private void ResolvePlayerInReachChanged()
    {
        if (!IsValidSearchState())
            return;

        if (!PlayerInReach)
            return;

        ResolvePlayerFound();
    }

    private void ResolvePlayerFound()
    {
        if (!IsValidSearchState())
            return;

        Debug.Log($"[NPC] {controller.NpcId}: player found during Search.");

        StopSearchTimer();

        if (animator != null)
            animator.SetBool("Searching", false);

        if (PlayerInReach)
        {
            controller.CurrentNpcState = NpcState.Dive;
            return;
        }

        if (chaseBehavior != null)
            chaseBehavior.ResetFruitlessSnapshotChase();

        controller.CurrentNpcState = NpcState.Chase;
    }

    private void ResolveSearchTimeOver()
    {
        if (controller.CurrentNpcState != NpcState.Search)
            return;

        StopSearchTimer();

        if (PlayerInReach)
        {
            controller.CurrentNpcState = NpcState.Dive;
            return;
        }

        if (PlayerInActualView)
        {
            if (chaseBehavior != null)
                chaseBehavior.ResetFruitlessSnapshotChase();

            controller.CurrentNpcState = NpcState.Chase;
            return;
        }

        if (chaseBehavior != null && chaseBehavior.SnapshotFruitlessChaseExceeded)
        {
            FailSearchToIdle("snapshot chase attempt maximum exceeded");
            return;
        }

        if (view != null && view.TryPrepareSearchTimeoutChaseTarget(out Vector3 target, out NpcChaseTargetKind targetKind))
        {
            Debug.Log($"[NPC] {controller.NpcId}: Search timed out, chasing {targetKind} target at {target}.");
            controller.CurrentNpcState = NpcState.Chase;
            return;
        }

        FailSearchToIdle("no actual or snapshot target available");
    }

    private void FailSearchToIdle(string reason)
    {
        Debug.Log($"[NPC] {controller.NpcId}: chase failed after Search ({reason}).");

        if (view != null)
            view.ClearAllPlayerSnapshots();

        if (chaseBehavior != null)
            chaseBehavior.ResetFruitlessSnapshotChase();

        if (anim != null)
            anim.PlaySearchToIdle();

        controller.NotifyNpcChaseFailed();
        controller.CurrentNpcState = NpcState.Idle;
    }

    private void StopSearchTimer()
    {
        searchStarted = false;

        if (timer != null)
        {
            timer.StopTimer(NpcTimerType.Search);
            timer.ResetTimer(NpcTimerType.Search);
        }
    }

    private bool IsValidSearchState()
    {
        LazyInstantiate();

        return searchStarted && controller != null && controller.CurrentNpcState == NpcState.Search;
    }

    private void LazyInstantiate()
    {
        if (view == null)
            view = GetComponent<NpcView>();

        if (timer == null)
            timer = GetComponent<NpcTimer>();

        if (controller == null)
            controller = GetComponent<NpcController>();

        if (nav == null)
            nav = GetComponent<NpcNavigate>();

        if (chaseBehavior == null)
            chaseBehavior = GetComponent<NpcChaseBehavior>();

        if (animator == null)
            animator = GetComponent<Animator>();

        if (anim == null)
            anim = GetComponent<NpcAnimationMachine>();
    }
}
