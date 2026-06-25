using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcNavigate : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] NpcType npcType;

    [Header("Debug")]
    [SerializeField] float updatePathIntv;
    [SerializeField] float newRage;

    // NPC components
    private NpcController controller
    {
        get
        {
            LazyInstantiate();
            return _controller;
        }
    }
        private NpcController _controller; // Do not access indented fields, Jill!
    private NavMeshAgent agent;

    // NPC Params
    private NpcData Data => controller.NpcData;
    private NpcRageState RageState
    {
        get
        {
            _rageState = controller.CurrentRageState;
            return _rageState;
        }
        set
        {
            if (value == _rageState) return;
            NpcRageState prevRageState = _rageState;
            _rageState = value;
            ResolveRageStateChange(prevRageState);
        }
    }
        private NpcRageState _rageState;
    private float CurrentSpeed
    {
        get
        {
            return _currentSpeed;
        }
        set
        {
            if (value == _currentSpeed) return;
            LazyInstantiate();
            agent.speed = value;
            _currentSpeed = value;
        }
    }
        private float _currentSpeed;
    private string Id => Data.npcId;
    private float ChaseSpeed => Data.chaseSpeed;
    private float MoveSpeed => Data.moveSpeed;
    private GameObject Target
    {
        get
        {
            LazyInstantiate();
            return controller.target;
        }
    }

    // NPC Movement delegate
    private delegate void NpcMovement(NpcRageState rageState);

    // cache
    private Coroutine navCoro;

    private void Awake()
    {
        LazyInstantiate();
    }

    private void Start()
    {
        StartChase(Target);
    }

    private void ResolveRageStateChange(NpcRageState prevRageState)
    {
        // decide nav behavior
        switch (RageState)
        {
            case NpcRageState.Enraged: StartChase(Target); break;
            case NpcRageState.Angry:
                if (prevRageState == NpcRageState.Enraged) StopChase();
                break;
            default: break;
        }

        Debug.Log($"[NPC] {Id}: RageState changes from {prevRageState} to {RageState}");

        // decide nav speed
        if (RageState == NpcRageState.Enraged) CurrentSpeed = ChaseSpeed;
        else CurrentSpeed = MoveSpeed;
    }

    public void StartChase(GameObject target)
    {
        Debug.Log($"[NPC] {Id}: Chase has started.");
        ToggleNav(true, target);
    }

    public void StopChase()
    {
        Debug.Log($"[NPC] {Id}: Chase has stopped.");
        ToggleNav(false);
    }

    private void ToggleNav(bool state, GameObject target = null)
    {
        if (state)
        {
            if (target == null)
            {
                Debug.LogWarning($"[NPC] {Id}: Cannot navigate to a null target");
                return;
            }
            navCoro = StartCoroutine(FollowTarget(target));
        }
        else // state == false
        {
            if (navCoro == null) return;
            if (target != null)
            {
                Debug.LogWarning($"[NPC] {Id}: Target not used because navigation has concluded.");
            }
            StopCoroutine(navCoro);
            navCoro = null;
        }
    }

    private IEnumerator FollowTarget(GameObject target)
    {
        while (enabled)
        {
            agent.SetDestination(target.transform.position);
            yield return new WaitForSeconds(updatePathIntv);
        }
    }

    private void LazyInstantiate()
    {
        // Used in properties and Awake()
        if (_controller == null) _controller = GetComponent<NpcController>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    //[ContextMenu("Test Rage Change")]
    //private void TestRageChange()
    //{
    //    RageManager rageManager = FindFirstObjectByType<RageManager>();
    //    rageManager.SetRage(Id, newRage);
    //}
}
