using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcIdleBehavior : MonoBehaviour
{
    private NpcNavigate nav;
    private NpcAnimationMachine anim;

    private void Awake()
    {
        nav = GetComponent<NpcNavigate>();
        anim = GetComponent<NpcAnimationMachine>();
    }

    public void Supervisor()
    {
        nav.Patrol(NpcState.Idle, false);
        // the above behavior is a placeholder.
        // In real game only the Cleaner needs to patrol
        // anim.PlayLocomotion();
    }

    public void Worker()
    {
        nav.Patrol(NpcState.Idle, false);
    }

    public void Cleaner()
    {
        nav.Patrol(NpcState.Idle, false);
    }

    public void Security()
    {
        nav.Patrol(NpcState.Idle, false);
    }

    public void ExitState()
    {
        nav.StopPatrol();
    }
}
