using System.Collections;
using UnityEngine;

public class NpcIdleBehavior : MonoBehaviour
{
    [Header("Seat")]
    [SerializeField] private SeatLogic assignedSeat;

    [SerializeField, Min(0f)]
    private float maxSeatSnapDistance = 0.45f;

    [Tooltip("Optional NPC bottom reference. If assigned, root is placed so this transform lands on the seat bottom point.")]
    [SerializeField] private Transform npcBottomReference;

    
     private readonly float standToSitFinishNormalizedTime = 0.98f;
     private readonly float standToSitTimeout = 4f;

    private NpcController controller;
    private NpcNavigate nav;
    private NpcAnimationMachine anim;

    private Coroutine idleRoutine;
    private bool waitingForSeatDestination;

    private bool HasSeat => assignedSeat != null;

    private void Awake()
    {
        LazyInstantiate();
    }

    private void OnEnable()
    {
        LazyInstantiate();

        if (nav != null)
            nav.DestinationReached += ResolveSeatDestinationReached;
    }

    private void OnDisable()
    {
        if (nav != null)
            nav.DestinationReached -= ResolveSeatDestinationReached;
    }

    public void Supervisor() => StartIdle();
    public void Worker() => StartIdle();
    public void Cleaner() => StartIdle();
    public void Security() => StartIdle();

    public void ExitState()
    {
        CleanupIdle();
    }

    private void StartIdle()
    {
        LazyInstantiate();

        CleanupIdle();

        if (HasSeat)
        {
            idleRoutine = StartCoroutine(SeatedIdleRoutine());
            return;
        }

        StartStandingIdleFallback();
    }

    private IEnumerator SeatedIdleRoutine()
    {
        if (!IsValidIdleState())
        {
            idleRoutine = null;
            yield break;
        }

        anim.PlayLocomotion();

        waitingForSeatDestination = true;

        nav.StartNavToPoint(assignedSeat.BottomPosition, false);

        float elapsed = 0f;

        while (true)
        {
            if (!IsValidIdleState())
            {
                idleRoutine = null;
                yield break;
            }

            if (IsCloseEnoughToSeat(maxSeatSnapDistance))
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        waitingForSeatDestination = false;

        if (!IsValidIdleState())
        {
            idleRoutine = null;
            yield break;
        }

        if (!IsCloseEnoughToSeat(maxSeatSnapDistance))
        {
            StartStandingIdleFallback();
            idleRoutine = null;
            yield break;
        }

        SnapToSeat();

        anim.PlayStandToSit();

        bool standToSitFinished = false;

        yield return anim.WaitForStateFinished(
            AnimState.TransitionStandToSit,
            success => standToSitFinished = success,
            standToSitFinishNormalizedTime,
            standToSitTimeout
        );

        if (!IsValidIdleState())
        {
            idleRoutine = null;
            yield break;
        }

        anim.PlayIdleSitting();

        idleRoutine = null;
    }

    private void ResolveSeatDestinationReached()
    {
        if (!waitingForSeatDestination)
            return;

        waitingForSeatDestination = false;
    }

    private bool IsCloseEnoughToSeat(float distance)
    {
        return GetFlatDistanceToSeat() <= distance;
    }

    private float GetFlatDistanceToSeat()
    {
        if (assignedSeat == null)
            return float.PositiveInfinity;

        Vector3 delta = assignedSeat.BottomPosition - transform.position;
        delta.y = 0f;

        return delta.magnitude;
    }

    private void SnapToSeat()
    {
        if (assignedSeat == null)
            return;

        Quaternion targetRotation = assignedSeat.SittingRotation;
        Vector3 targetRootPosition = ResolveRootPositionForSeat(targetRotation);

        nav.StopNav();
        nav.WarpTo(targetRootPosition, targetRotation);

        Debug.Log($"[NPC Idle] {name}: snapped to seat {assignedSeat.name}.");
    }

    private Vector3 ResolveRootPositionForSeat(Quaternion targetRotation)
    {
        if (assignedSeat == null)
            return transform.position;

        if (npcBottomReference == null)
            return assignedSeat.BottomPosition;

        Vector3 localBottomOffset = transform.InverseTransformPoint(npcBottomReference.position);
        Vector3 rotatedOffset = targetRotation * localBottomOffset;

        return assignedSeat.BottomPosition - rotatedOffset;
    }



    private void StartStandingIdleFallback()
    {
        anim.PlayLocomotion();

        nav.Patrol(NpcState.Idle, false);
    }

    private void CleanupIdle()
    {
        waitingForSeatDestination = false;

        if (idleRoutine != null)
        {
            StopCoroutine(idleRoutine);
            idleRoutine = null;
        }

        if (nav != null)
            nav.StopPatrol();
    }

    private bool IsValidIdleState()
    {
        return controller != null && controller.CurrentNpcState == NpcState.Idle;
    }

    private void LazyInstantiate()
    {
        if (controller == null)
            controller = GetComponent<NpcController>();

        if (nav == null)
            nav = GetComponent<NpcNavigate>();

        if (anim == null)
            anim = GetComponent<NpcAnimationMachine>();
    }
}