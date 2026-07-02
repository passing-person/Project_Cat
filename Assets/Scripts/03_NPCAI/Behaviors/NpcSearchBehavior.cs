using UnityEngine;

public class NpcSearchBehavior : MonoBehaviour
{
    private NpcView view;
    private NpcTimer timer;
    private NpcController controller;
    private NpcNavigate nav;

    private NpcAnimationMachine anim;
    private Animator animator;

    // cache
    private bool PlayerInView => view.PlayerInView;
    private bool PlayerInReach => view.PlayerInReach;

    private bool searchStarted;

    private void Awake()
    {
        LazyInstantiate();
    }

    private void OnEnable()
    {
        LazyInstantiate();

        view.PlayerInViewFlagChange += ResolvePlayerInViewChanged;
        view.PlayerInReachFlagChange += ResolvePlayerInReachChanged;
    }

    private void OnDisable()
    {
        if (view != null)
        {
            view.PlayerInViewFlagChange -= ResolvePlayerInViewChanged;
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

        timer.StopTimer(NpcTimerType.Search);
        timer.ResetTimer(NpcTimerType.Search);

        anim.PlayLocomotion();
    }

    private void StartSearch()
    {
        LazyInstantiate();

        if (searchStarted)
            return;

        searchStarted = true;

        nav.StopPatrol();
        nav.StopNav();

        anim.PlaySearch();

        if (PlayerInView)
        {
            controller.CurrentNpcState = NpcState.Chase;
            return;
        }

        timer.StartTimer(NpcTimerType.Search, ResolveSearchTimeOver);
    }

    private void ResolvePlayerInViewChanged()
    {
        if (!IsValidSearchState())
            return;

        if (!PlayerInView)
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

        Debug.Log($"[NPC] {controller.NpcId}: Player found during Search.");

        searchStarted = false;

        timer.StopTimer(NpcTimerType.Search);
        timer.ResetTimer(NpcTimerType.Search);

        if (animator != null)
            animator.SetBool("Searching", false);

        if (PlayerInReach)
        {
            controller.CurrentNpcState = NpcState.Dive;
            return;
        }

        controller.CurrentNpcState = NpcState.Chase;
    }

    private void ResolveSearchTimeOver()
    {
        if (controller.CurrentNpcState != NpcState.Search)
            return;

        searchStarted = false;

        if (PlayerInView)
        {
            controller.CurrentNpcState = NpcState.Chase;
            return;
        }

        anim.PlaySearchToIdle();
        controller.CurrentNpcState = NpcState.Idle;
    }

    private bool IsValidSearchState()
    {
        LazyInstantiate();

        return searchStarted && controller.CurrentNpcState == NpcState.Search;
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

        if (animator == null)
            animator = GetComponent<Animator>();

        if (anim == null)
            anim = GetComponent<NpcAnimationMachine>();
    }
}