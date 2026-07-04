using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(-10000)]
public class NavMeshAgentPlacementFixer : MonoBehaviour
{
    [Header("NavMesh Agent Safety")]
    public bool fixOnAwake = true;
    public bool fixOnStart = true;
    public float sampleRadius = 6f;
    public bool logResult = true;

    private void Awake()
    {
        if (fixOnAwake)
        {
            FixAllAgents();
        }
    }

    private void Start()
    {
        if (fixOnStart)
        {
            FixAllAgents();
        }
    }

    [ContextMenu("Fix All NavMeshAgents")]
    public void FixAllAgents()
    {
        NavMeshAgent[] agents = FindObjectsOfType<NavMeshAgent>(true);
        int fixedCount = 0;
        int missingCount = 0;

        foreach (NavMeshAgent agent in agents)
        {
            if (agent == null)
            {
                continue;
            }

            if (TryPlaceAgentOnNavMesh(agent))
            {
                fixedCount++;
            }
            else
            {
                missingCount++;
            }
        }

        if (logResult)
        {
            Debug.Log($"[NavMeshAgentPlacementFixer] Checked {agents.Length} agents. Placed={fixedCount}, MissingNavMesh={missingCount}", this);
        }
    }

    private bool TryPlaceAgentOnNavMesh(NavMeshAgent agent)
    {
        if (!agent.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (!NavMesh.SamplePosition(agent.transform.position, out NavMeshHit hit, sampleRadius, NavMesh.AllAreas))
        {
            Debug.LogWarning($"[NavMeshAgentPlacementFixer] No NavMesh near {agent.name}. Build NavMesh in MainScene first.", agent);
            return false;
        }

        bool wasEnabled = agent.enabled;
        if (!wasEnabled)
        {
            agent.enabled = true;
        }

        if (agent.isOnNavMesh)
        {
            agent.Warp(hit.position);
            return true;
        }

        agent.enabled = false;
        agent.transform.position = hit.position;
        agent.enabled = true;

        if (agent.isOnNavMesh)
        {
            agent.Warp(hit.position);
            return true;
        }

        Debug.LogWarning($"[NavMeshAgentPlacementFixer] Failed to place {agent.name} on NavMesh even after sampling.", agent);
        return false;
    }
}
