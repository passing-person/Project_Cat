using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NpcNavigate : MonoBehaviour
{
    [Header("Params")]
    [SerializeField, Range(0f, .5f)] float updatePathIntv;

    [Header("Path Mapping (per state)")]
    [SerializeField] private List<StatePathMapping> statePaths = new();

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

    // API
    public bool IsNavigating => navCoro != null;

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

    // actions
    public event Action OnNavigationOver;
    public event Action DestinationReached;

    // cache
    private Coroutine navCoro;
    private GameObject player;
    private Dictionary<NpcState, PatrolPath> pathDict;
    private Coroutine patrolCoro;

    private void Awake()
    {
        LazyInstantiate();
        BuildPathDictionary();
    }

    public void ToggleChasePlayer(bool state)
    {
        if (state) StartNav(player, true);
        else StopNav();
    }

    public void StartNav(GameObject target, bool isChasing)
    {
        StopNav();
        Debug.Log($"[NPC] {Id}: Chase has started.");
        ToggleSpeed(isChasing);
        ToggleNav(target, true);
    }

    public void StartNavToPoint(Vector3 target, bool isChasing)
    {
        StopNav();
        Debug.Log($"[NPC] {Id}: Chase has started.");
        ToggleSpeed(isChasing);
        ToggleNav(target, true);
    }

    public void StopNav()
    {
        if (navCoro != null)
        {
            StopCoroutine(navCoro);
            navCoro = null;
            Debug.Log($"[NPC] {Id}: Navigation stopped.");
        }
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }
        agent.enabled = false;
        OnNavigationOver?.Invoke();
    }

    /// <summary>
    /// Start following the path assigned to the given NPC state.
    /// If no path is assigned, does nothing.
    /// </summary>
    public void NavigateAlongPath(NpcState state, bool isChasing)
    {
        StopNav();

        if (!pathDict.TryGetValue(state, out PatrolPath path))
            return;

        ToggleSpeed(isChasing);

        navCoro = StartCoroutine(FollowPath(path));
    }

    /// <summary>
    /// Start a continuous patrol along the path assigned to the given state.
    /// The patrol will loop forever, restarting the path each time it ends.
    /// </summary>
    public void Patrol(NpcState state, bool isChasing)
    {
        StopPatrol();
        patrolCoro = StartCoroutine(PatrolRoutine(state, isChasing));
    }

    /// <summary>
    /// Stop the current patrol.
    /// </summary>
    public void StopPatrol()
    {
        if (patrolCoro != null)
        {
            StopCoroutine(patrolCoro);
            patrolCoro = null;
        }
        StopNav(); // Also stop any ongoing navigation
    }

    private void ToggleNav(GameObject target, bool state)
    {
        if (state)
        {
            agent.enabled = true;
            if (target == null)
            {
                Debug.LogWarning($"[NPC] {Id}: Cannot navigate because player is null.");
                return;
            }
            navCoro = StartCoroutine(FollowTarget(target));
        }
        else // state == false
        {
            if (navCoro == null) return;
            StopCoroutine(navCoro);
            navCoro = null;
            agent.enabled = false;
        }
    }

    private void ToggleNav(Vector3 target, bool state)
    {
        if (state)
        {
            agent.enabled = true;
            navCoro = StartCoroutine(NavToPoint(target));
        }
        else // state == false
        {
            if (navCoro == null) return;
            StopCoroutine(navCoro);
            navCoro = null;
            agent.enabled = false;
        }
    }

    private void ToggleSpeed(bool isChasing = false)
    {
        CurrentSpeed = isChasing ? ChaseSpeed : MoveSpeed;
    }

    private void LazyInstantiate()
    {
        // Used in properties and Awake()
        if (_controller == null) _controller = GetComponent<NpcController>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (player == null) player = FindFirstObjectByType<PlayerController>().gameObject;
    }

    private void BuildPathDictionary()
    {
        pathDict = new Dictionary<NpcState, PatrolPath>();
        foreach (var mapping in statePaths)
        {
            if (!pathDict.ContainsKey(mapping.state))
                pathDict.Add(mapping.state, mapping.path);
        }
    }

    private IEnumerator FollowTarget(GameObject target)
    {
        while (enabled && target != null)
        {
            agent.SetDestination(target.transform.position);
            yield return new WaitForSeconds(updatePathIntv);
        }
    }

    private IEnumerator NavToPoint(Vector3 target)
    {
        agent.SetDestination(target);

        while (agent.pathPending ||
               agent.remainingDistance > agent.stoppingDistance)
        {
            yield return null;
        }

        agent.ResetPath();
        navCoro = null;
        DestinationReached?.Invoke();
    }

    private IEnumerator FollowPath(PatrolPath path)
    {
        agent.enabled = true;

        yield return null;

        int currentIndex = 0;
        int count = path.Waypoints.Count;

        while (enabled)
        {
            Vector3 target = path.GetWorldPoint(currentIndex);

            agent.SetDestination(target);

            while (agent.pathPending ||
                   agent.remainingDistance > agent.stoppingDistance)
            {
                yield return null;
            }

            if (path.IsClosed)
            {
                currentIndex = (currentIndex + 1) % count;
            }
            else
            {
                if (++currentIndex >= count)
                    break;
            }
        }

        agent.ResetPath();
        navCoro = null;
        OnNavigationOver?.Invoke();
    }

    private IEnumerator PatrolRoutine(NpcState state, bool isChasing)
    {
        while (enabled)
        {
            if (!pathDict.TryGetValue(state, out PatrolPath path) || path == null)
            {
                Debug.Log($"[NPC] {Id}: No patrol path for {state}.");
                yield break;
            }

            ToggleSpeed(isChasing);

            yield return FollowPath(path);

            Debug.Log($"[NPC] {Id}: Patrol loop cycle complete.");

            yield return null;
        }
    }

    [System.Serializable]
    public class StatePathMapping
    {
        public NpcState state;
        public PatrolPath path;
    }
}