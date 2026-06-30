using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcChaseBehavior : MonoBehaviour
{
    private NpcNavigate nav;

    private void Awake()
    {
        nav = GetComponent<NpcNavigate>();
    }

    public void Supervisor()
    {
        nav.ToggleChasePlayer(true);
    }

    public void Worker()
    {
        nav.ToggleChasePlayer(true);
    }

    public void Cleaner()
    {
        nav.ToggleChasePlayer(true);
    }

    public void Security()
    {
        nav.ToggleChasePlayer(true);
    }
}
