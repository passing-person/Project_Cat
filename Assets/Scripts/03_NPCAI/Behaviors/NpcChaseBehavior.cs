using System.Collections;
using UnityEngine;

public class NpcChaseBehavior : MonoBehaviour
{
    private NpcNavigate nav;
    private NpcController controller;
    private NpcTimer timer;
    private NpcView view;

    private bool PlayerInView => view.PlayerInView;

    private Vector3 LastKnownPosition;
    private GameObject player;

    private bool navigatingToLastKnownPos;
    private bool chaseTimerStarted;

    private Coroutine chaseRoutine;

    private enum ChaseMode
    {
        None,
        ToLastKnownPosition,
        ToPlayer
    }

    private ChaseMode chaseMode = ChaseMode.None;

    private void Awake()
    {
        LazyInstantiate();
    }

    private void OnEnable()
    {
        LazyInstantiate();
        nav.DestinationReached += ResolveDestinationReached;
    }

    private void OnDisable()
    {
        nav.DestinationReached -= ResolveDestinationReached;
    }

    public void Supervisor()
    {
        ChaseAndFindPlayer();
    }

    public void Worker()
    {
        ChaseAndFindPlayer();
    }

    public void Cleaner()
    {
        ChaseAndFindPlayer();
    }

    public void Security()
    {
        ChaseAndFindPlayer();
    }

    public void ExitState()
    {
        CleanupChase(stopNav: true, stopTimer: true);
    }

    private void ChaseAndFindPlayer()
    {
        StartChaseTimerIfNeeded();

        if (chaseRoutine != null)
            return;

        chaseRoutine = StartCoroutine(ChaseRoutine());
    }

    private IEnumerator ChaseRoutine()
    {
        if (PlayerInView)
        {
            SwitchToPlayerChase();
        }
        else
        {
            SwitchToLastKnownPositionChase();
        }

        // Important:
        // NpcController currently calls enter behavior before assigning _npcState.
        // Wait one frame so CurrentNpcState is actually Chase.
        yield return null;

        while (controller.CurrentNpcState == NpcState.Chase)
        {
            if (chaseMode == ChaseMode.ToLastKnownPosition && PlayerInView)
            {
                SwitchToPlayerChase();
            }

            yield return null;
        }

        chaseRoutine = null;
    }

    private void SwitchToLastKnownPositionChase()
    {
        if (chaseMode == ChaseMode.ToLastKnownPosition)
            return;

        LastKnownPosition = player.transform.position;

        navigatingToLastKnownPos = true;
        chaseMode = ChaseMode.ToLastKnownPosition;

        nav.StartNavToPoint(LastKnownPosition, true);

        Debug.Log($"[NPC] {controller.NpcId}: chasing to last known player position.");
    }

    private void SwitchToPlayerChase()
    {
        if (chaseMode == ChaseMode.ToPlayer)
            return;

        navigatingToLastKnownPos = false;
        chaseMode = ChaseMode.ToPlayer;

        // This cancels NavToPoint and starts FollowTarget(player).
        nav.ToggleChasePlayer(true);

        Debug.Log($"[NPC] {controller.NpcId}: player acquired, switching chase target to player.");
    }

    private void StartChaseTimerIfNeeded()
    {
        if (chaseTimerStarted)
            return;

        chaseTimerStarted = true;
        timer.StartTimer(NpcTimerType.Chase, ResolveChaseTimeOver);
    }

    private void ResolveChaseTimeOver()
    {
        if (controller.CurrentNpcState != NpcState.Chase)
            return;

        Debug.Log($"[NPC] {controller.NpcId}: chase time over.");

        TransitionOutOfChase(NpcState.Cooldown);
    }

    private void ResolveDestinationReached()
    {
        if (controller.CurrentNpcState != NpcState.Chase)
            return;

        // Ignore stale destination events after we have already switched to player chase.
        if (chaseMode != ChaseMode.ToLastKnownPosition)
            return;

        if (!navigatingToLastKnownPos)
            return;

        if (PlayerInView)
        {
            SwitchToPlayerChase();
            return;
        }

        Debug.Log($"[NPC] {controller.NpcId}: reached last known position, player not seen, switching to Search.");

        TransitionOutOfChase(NpcState.Search);
    }

    private void TransitionOutOfChase(NpcState nextState)
    {
        CleanupChase(stopNav: true, stopTimer: true);
        controller.CurrentNpcState = nextState;
    }

    private void CleanupChase(bool stopNav, bool stopTimer)
    {
        if (chaseRoutine != null)
        {
            StopCoroutine(chaseRoutine);
            chaseRoutine = null;
        }

        navigatingToLastKnownPos = false;
        chaseMode = ChaseMode.None;

        if (stopTimer && chaseTimerStarted)
        {
            timer.StopTimer(NpcTimerType.Chase);
            timer.ResetTimer(NpcTimerType.Chase);
            chaseTimerStarted = false;
        }

        if (stopNav)
        {
            nav.ToggleChasePlayer(false);
        }
    }

    private void LazyInstantiate()
    {
        if (nav == null)
            nav = GetComponent<NpcNavigate>();

        if (controller == null)
            controller = GetComponent<NpcController>();

        if (timer == null)
            timer = GetComponent<NpcTimer>();

        if (view == null)
            view = GetComponent<NpcView>();

        if (player == null)
            player = FindFirstObjectByType<PlayerController>().gameObject;
    }
}