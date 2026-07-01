using UnityEngine;

public class NpcDiveBehavior : MonoBehaviour
{
    private NpcNavigate nav;
    private NpcController controller;
    private NpcTimer timer;
    private NpcView view;

    private PlayerController player;

    private bool diveStarted;
    private bool diveActive;

    private bool PlayerInReach => view.PlayerInReach;

    private void Awake()
    {
        LazyInstantiate();
    }

    private void OnEnable()
    {
        LazyInstantiate();

        view.PlayerInReachFlagChange += ResolvePlayerInReachChanged;
    }

    private void OnDisable()
    {
        if (view != null)
            view.PlayerInReachFlagChange -= ResolvePlayerInReachChanged;
    }

    public void Supervisor()
    {
        StartDive();
    }

    public void Worker()
    {
        StartDive();
    }

    public void Cleaner()
    {
        StartDive();
    }

    public void Security()
    {
        StartDive();
    }

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
        diveActive = false;

        // Dive is non-navigation behavior.
        // Stop both patrol and chase/path navigation.
        nav.StopPatrol();

        // TODO: play dive wind-up animation
        Debug.Log($"[NPC] {controller.NpcId}: Dive wind-up started.");

        timer.StartTimer(NpcTimerType.DiveWindUp, ResolveDiveWindUpOver);
    }

    private void ResolveDiveWindUpOver()
    {
        if (!IsValidDiveState())
            return;

        diveActive = true;

        // TODO: play active dive animation
        Debug.Log($"[NPC] {controller.NpcId}: Dive active.");

        // If the player is already in reach when wind-up ends, catch immediately.
        if (PlayerInReach)
        {
            ResolvePlayerCaught();
            return;
        }

        timer.StartTimer(NpcTimerType.Dive, ResolveDiveOver);
    }

    private void ResolveDiveOver()
    {
        if (!IsValidDiveState())
            return;

        if (PlayerInReach)
        {
            ResolvePlayerCaught();
            return;
        }

        ResolveDiveMissed();
    }

    private void ResolvePlayerInReachChanged()
    {
        if (!IsValidDiveState())
            return;

        if (!diveActive)
            return;

        if (!PlayerInReach)
            return;

        ResolvePlayerCaught();
    }

    private void ResolvePlayerCaught()
    {
        if (!IsValidDiveState())
            return;

        Debug.Log($"[NPC] {controller.NpcId}: Player caught by dive.");

        CleanupDive();

        // PlayerCaught ending. Do not enter Cooldown here.
        controller.OnPlayerCaught(player);
    }

    private void ResolveDiveMissed()
    {
        if (!IsValidDiveState())
            return;

        Debug.Log($"[NPC] {controller.NpcId}: Dive missed. Entering Cooldown.");

        CleanupDive();

        // CooldownBehavior should infer this came from Dive and use DiveCooldown.
        controller.CurrentNpcState = NpcState.Cooldown;
    }

    private bool IsValidDiveState()
    {
        LazyInstantiate();

        return diveStarted && controller.CurrentNpcState == NpcState.Dive;
    }

    private void CleanupDive()
    {
        diveStarted = false;
        diveActive = false;

        if (timer != null)
        {
            StopAndResetTimer(NpcTimerType.DiveWindUp);
            StopAndResetTimer(NpcTimerType.Dive);
        }

        if (nav != null)
            nav.StopNav();
    }

    private void StopAndResetTimer(NpcTimerType timerType)
    {
        timer.StopTimer(timerType);
        timer.ResetTimer(timerType);
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
            player = FindFirstObjectByType<PlayerController>();
    }
}