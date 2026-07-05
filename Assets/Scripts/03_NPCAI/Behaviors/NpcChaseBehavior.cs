using System.Collections;
using UnityEngine;

public class NpcChaseBehavior : MonoBehaviour
{
    [Header("Snapshot Chase")]
    [Tooltip("Maximum consecutive attempts the NPC may spend chasing snapshot positions without reacquiring actual view. 0 means unlimited.")]
    [SerializeField, Min(0f)] private int maxFruitlessChaseAttempt = 3;

    [Tooltip("Minimum movement of the active snapshot before the NavMesh destination is refreshed.")]
    [SerializeField, Min(0f)] private float snapshotDestinationRefreshDistance = 0.15f;

    [Header("Dive Transition")]
    [Tooltip("If true, Chase explicitly checks the current NpcView dive flags on entry and before chase navigation. This fixes the case where PlayerInReach was already true before Chase began, so no PlayerInReachFlagChange event fires.")]
    [SerializeField] private bool transitionToDiveImmediatelyWhenAvailable = true;

    [Tooltip("If true, Chase calls NpcView.Refresh() before checking DiveRequestIsValid so stale same-frame view data cannot block an immediate Chase -> Dive transition.")]
    [SerializeField] private bool refreshViewBeforeImmediateDiveCheck = true;

    private NpcNavigate nav;
    private NpcController controller;
    private NpcTimer timer;
    private NpcView view;
    private NpcAnimationMachine anim;

    private bool PlayerInView => view.PlayerInView;
    private bool PlayerInActualView => view.PlayerInActualView;

    private bool navigatingToSnapshot;
    private bool chaseTimerStarted;

    private int fruitlessSnapshotChaseAttempt;


    private Vector3 currentSnapshotDestination;
    private bool hasCurrentSnapshotDestination;

    private Coroutine chaseRoutine;

    private enum ChaseMode
    {
        None,
        ToSnapshot,
        ToPlayer
    }

    private ChaseMode chaseMode = ChaseMode.None;

    public float MaxFruitlessChaseAttempt => maxFruitlessChaseAttempt;
    public float FruitlessSnapshotChaseTime => fruitlessSnapshotChaseAttempt;
    public bool SnapshotFruitlessChaseExceeded =>
        fruitlessSnapshotChaseAttempt > maxFruitlessChaseAttempt;

    private void Awake()
    {
        LazyInstantiate();
    }

    private void OnEnable()
    {
        LazyInstantiate();

        if (nav != null)
            nav.DestinationReached += ResolveDestinationReached;
    }

    private void OnDisable()
    {
        if (nav != null)
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

    public void ResetFruitlessSnapshotChase()
    {
        fruitlessSnapshotChaseAttempt = 0;
    }

    private void ChaseAndFindPlayer()
    {
        LazyInstantiate();

        // Important:
        // PlayerInReach / DiveRequestIsValid can already be true before the NPC enters Chase.
        // In that case NpcView will not emit PlayerInReachFlagChange again, so the state
        // machine may not get a fresh chance to resolve Chase -> Dive. Check it explicitly.
        if (TryTransitionToDiveIfAvailable("chase enter"))
            return;

        StartChaseTimerIfNeeded();

        if (anim != null)
            anim.PlayLocomotion();

        if (chaseRoutine != null)
            return;

        chaseRoutine = StartCoroutine(ChaseRoutine());
    }

    private bool TryTransitionToDiveIfAvailable(string reason)
    {
        if (!transitionToDiveImmediatelyWhenAvailable)
            return false;

        if (controller == null || controller.CurrentNpcState != NpcState.Chase)
            return false;

        if (view == null)
            return false;

        if (refreshViewBeforeImmediateDiveCheck)
            view.Refresh();

        // Refresh() can invoke view flag events. If those events already caused the state
        // machine to leave Chase, treat this as handled and stop the chase entry/loop.
        if (controller.CurrentNpcState != NpcState.Chase)
            return true;

        if (!view.DiveRequestIsValid)
            return false;

        Debug.Log(
            $"[NPC] {controller.NpcId}: dive is available during {reason}, " +
            "switching from Chase to Dive immediately."
        );

        if (!view.PlayerHidden)
            TransitionOutOfChase(NpcState.Dive);
        return true;
    }

    private IEnumerator ChaseRoutine()
    {
        if (!PlayerInView)
        {
            // A rage-triggered chase should still begin with a snapshot if possible.
            if (view != null && view.CapturePlayerPositionSnapshot())
                Debug.Log($"[NPC] {controller.NpcId}: chase started from fresh player snapshot.");
        }

        yield return null;

        NpcChaseTargetKind cacheChaseTargetKind = NpcChaseTargetKind.Snapshot;
        while (controller.CurrentNpcState == NpcState.Chase)
        {
            if (TryTransitionToDiveIfAvailable("chase loop"))
                yield break;

            if (view == null || !view.TryGetKnownPlayerPosition(out Vector3 targetPosition, out NpcChaseTargetKind targetKind))
            {
                Debug.Log($"[NPC] {controller.NpcId}: no actual or snapshot target left, switching to Search.");
                TransitionOutOfChase(NpcState.Search);
                yield break;
            }

            ResolveFruitlessChaseCapacity(prev: cacheChaseTargetKind, current: targetKind);
            cacheChaseTargetKind = targetKind;

            if (targetKind == NpcChaseTargetKind.Actual)
            {
                SwitchToPlayerChase();
            }
            else
            {
                SwitchToSnapshotChase(targetPosition);

                if (SnapshotFruitlessChaseExceeded)
                {
                    Debug.Log($"[NPC] {controller.NpcId}: snapshot chase exceeded {maxFruitlessChaseAttempt} time(s), switching to Search.");
                    view.ClearActivePlayerSnapshot();
                    TransitionOutOfChase(NpcState.Search);
                    yield break;
                }
            }

            yield return null;
        }

        chaseRoutine = null;
    }

    private void SwitchToSnapshotChase(Vector3 snapshotPosition)
    {
        bool destinationChanged = !hasCurrentSnapshotDestination ||
            (snapshotPosition - currentSnapshotDestination).sqrMagnitude > snapshotDestinationRefreshDistance * snapshotDestinationRefreshDistance;

        if (chaseMode == ChaseMode.ToSnapshot && !destinationChanged)
            return;

        if (SnapshotFruitlessChaseExceeded)
            return;

        currentSnapshotDestination = snapshotPosition;
        hasCurrentSnapshotDestination = true;
        navigatingToSnapshot = true;
        chaseMode = ChaseMode.ToSnapshot;

        if (nav != null)
        {
            nav.ToggleChasePlayer(false);
            nav.StartNavToPoint(currentSnapshotDestination, true);
        }

        if (anim != null)
            anim.PlayLocomotion();

        Debug.Log($"[NPC] {controller.NpcId}: chasing snapshot at {currentSnapshotDestination}.");
    }

    private void SwitchToPlayerChase()
    {
        if (chaseMode == ChaseMode.ToPlayer)
            return;

        navigatingToSnapshot = false;
        hasCurrentSnapshotDestination = false;
        fruitlessSnapshotChaseAttempt = 0;
        chaseMode = ChaseMode.ToPlayer;

        if (nav != null)
            nav.ToggleChasePlayer(true);

        if (anim != null)
            anim.PlayLocomotion();

        Debug.Log($"[NPC] {controller.NpcId}: actual player acquired, chasing player.");
    }

    private void ResolveFruitlessChaseCapacity(NpcChaseTargetKind prev, NpcChaseTargetKind current)
    {

        if (prev == current) return;
        if (current == NpcChaseTargetKind.Snapshot)
            fruitlessSnapshotChaseAttempt++;
        else if (current == NpcChaseTargetKind.Actual)
            ResetFruitlessSnapshotChase();
        else
        {
            Debug.LogWarning($"[NPC] {controller.NpcId}: unidentified target of type NpcChaseTargetKind.None. Skip counter update.");
            return;
        }
    }

    private void StartChaseTimerIfNeeded()
    {
        if (chaseTimerStarted || timer == null)
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

        if (chaseMode != ChaseMode.ToSnapshot || !navigatingToSnapshot)
            return;

        if (PlayerInActualView)
        {
            ResetFruitlessSnapshotChase();
            SwitchToPlayerChase();
            return;
        }

        Debug.Log($"[NPC] {controller.NpcId}: reached snapshot, player not seen, switching to Search.");

        view.ClearActivePlayerSnapshot();
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

        navigatingToSnapshot = false;
        chaseMode = ChaseMode.None;
        hasCurrentSnapshotDestination = false;
        fruitlessSnapshotChaseAttempt = 0;

        if (stopTimer && chaseTimerStarted && timer != null)
        {
            timer.StopTimer(NpcTimerType.Chase);
            timer.ResetTimer(NpcTimerType.Chase);
            chaseTimerStarted = false;
        }

        if (stopNav && nav != null)
            nav.ToggleChasePlayer(false);
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

        if (anim == null)
            anim = GetComponent<NpcAnimationMachine>();
    }
}
