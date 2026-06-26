using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcNavigate : MonoBehaviour
{
    [Header("Params")]
    [SerializeField] NpcType npcType;
    [SerializeField, Range(0f, .5f)] float updatePathIntv;

    [Header("Debug")]
    [SerializeField] bool debugIsChasing;
    [SerializeField, Range(1f, 100f)] float debugSpeedMult;

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
            agent.speed = value * debugSpeedMult;
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
        StartNav(Target, debugIsChasing);
    }

    public void StartNav(GameObject target, bool isChasing)
    {
        Debug.Log($"[NPC] {Id}: Chase has started.");
        ToggleSpeed(isChasing);
        ToggleNav(true, target);
    }

    public void StopNav()
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

    private void ToggleSpeed(bool isChasing = false)
    {
        CurrentSpeed = isChasing ? ChaseSpeed : MoveSpeed;
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
}