using System.Collections;
using UnityEngine;

public class NpcChaseBehavior : MonoBehaviour
{
    [Header("Snapshot Chase")]
    [Tooltip("Maximum consecutive time the NPC may spend chasing snapshot positions without reacquiring actual view. 0 means unlimited.")]
    [SerializeField, Min(0f)] private float maxFruitlessChaseTime = 3f;

    [Tooltip("Minimum movement of the active snapshot before the NavMesh destination is refreshed.")]
    [SerializeField, Min(0f)] private float snapshotDestinationRefreshDistance = 0.15f;

    private NpcNavigate nav;
    private NpcController controller;
    private NpcTimer timer;
    private NpcView view;
    private NpcAnimationMachine anim;

    private bool PlayerInView => view.PlayerInView;
    private bool PlayerInActualView => view.PlayerInActualView;

    private bool navigatingToSnapshot;
    private bool chaseTimerStarted;

    private float fruitlessSnapshotChaseTime;
    private bool snapshotFruitlessChaseExceeded;

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

    public float MaxFruitlessChaseTime => maxFruitlessChaseTime;
    public float FruitlessSnapshotChaseTime => fruitlessSnapshotChaseTime;
    public bool SnapshotFruitlessChaseExceeded => snapshotFruitlessChaseExceeded;

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
        fruitlessSnapshotChaseTime = 0f;
        snapshotFruitlessChaseExceeded = false;
    }

    private void ChaseAndFindPlayer()
    {
        LazyInstantiate();
        StartChaseTimerIfNeeded();

        if (anim != null)
            anim.PlayLocomotion();

        if (chaseRoutine != null)
            return;

        chaseRoutine = StartCoroutine(ChaseRoutine());
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

        while (controller.CurrentNpcState == NpcState.Chase)
        {
            if (view == null || !view.TryGetKnownPlayerPosition(out Vector3 targetPosition, out NpcPlayerTargetKind targetKind))
            {
                Debug.Log($"[NPC] {controller.NpcId}: no actual or snapshot target left, switching to Search.");
                TransitionOutOfChase(NpcState.Search);
                yield break;
            }

            if (targetKind == NpcPlayerTargetKind.Actual)
            {
                ResetFruitlessSnapshotChase();
                SwitchToPlayerChase();
            }
            else
            {
                SwitchToSnapshotChase(targetPosition);
                TickFruitlessSnapshotChaseTimer();

                if (snapshotFruitlessChaseExceeded)
                {
                    Debug.Log($"[NPC] {controller.NpcId}: snapshot chase exceeded {maxFruitlessChaseTime:0.00}s, switching to Search.");
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
        chaseMode = ChaseMode.ToPlayer;

        if (nav != null)
            nav.ToggleChasePlayer(true);

        if (anim != null)
            anim.PlayLocomotion();

        Debug.Log($"[NPC] {controller.NpcId}: actual player acquired, chasing player.");
    }

    private void TickFruitlessSnapshotChaseTimer()
    {
        if (maxFruitlessChaseTime <= 0f)
            return;

        fruitlessSnapshotChaseTime += Time.deltaTime;

        if (fruitlessSnapshotChaseTime >= maxFruitlessChaseTime)
            snapshotFruitlessChaseExceeded = true;
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
