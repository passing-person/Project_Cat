using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcIdleBehavior : MonoBehaviour
{
    private NpcNavigate nav;

    private void Awake()
    {
        nav = GetComponent<NpcNavigate>();
    }

    public void Supervisor()
    {
        nav.Patrol(NpcState.Idle, false);
    }

    public void Worker()
    {

    }

    public void Cleaner()
    {

    }

    public void Security()
    {

    }
}
