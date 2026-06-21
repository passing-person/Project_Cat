using System.Collections.Generic;
using UnityEngine;

public class RageManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private UIManager uiManager;

    private readonly Dictionary<string, NpcController> npcs = new Dictionary<string, NpcController>();
    private readonly Dictionary<string, float> rageByNpcId = new Dictionary<string, float>();

    public void RegisterNpc(string npcId, NpcController npc)
    {
        if (string.IsNullOrEmpty(npcId) || npc == null)
            return;

        npcs[npcId] = npc;

        if (!rageByNpcId.ContainsKey(npcId))
            rageByNpcId[npcId] = 0f;

        UpdateScoreMultiplierFromRage();
    }

    public void UnregisterNpc(string npcId)
    {
        if (string.IsNullOrEmpty(npcId))
            return;

        npcs.Remove(npcId);
        rageByNpcId.Remove(npcId);
        UpdateScoreMultiplierFromRage();
    }

    public float GetRage(string npcId)
    {
        float rage;
        return rageByNpcId.TryGetValue(npcId, out rage) ? rage : 0f;
    }

    public NpcRageState GetRageState(string npcId)
    {
        NpcController npc;
        if (!npcs.TryGetValue(npcId, out npc))
            return NpcRageState.Calm;

        return CalculateRageState(GetRage(npcId), npc.NpcData);
    }

    public float GetAverageRage()
    {
        float total = 0f;
        int count = 0;

        foreach (KeyValuePair<string, NpcController> pair in npcs)
        {
            if (pair.Value == null || !pair.Value.CanReceiveRage)
                continue;

            total += GetRage(pair.Key);
            count++;
        }

        if (count == 0)
            return 0f;

        return total / count;
    }

    public void AddRageByMischief(MischiefContext context)
    {
        List<NpcController> affectedNpcs = GetAffectedNpcs(context);

        foreach (NpcController npc in affectedNpcs)
        {
            AddRage(npc.NpcId, context.BaseRageAmount);
        }

        if (affectedNpcs.Count > 0 && scoreManager != null)
        {
            scoreManager.StartScoring();
        }
    }

    public RageResult AddRage(string npcId, float amount)
    {
        NpcController npc;
        if (!npcs.TryGetValue(npcId, out npc) || npc == null)
            return default(RageResult);

        float previousRage = GetRage(npcId);
        NpcRageState previousState = CalculateRageState(previousRage, npc.NpcData);

        float currentRage = Mathf.Clamp(previousRage + amount, 0f, 100f);
        rageByNpcId[npcId] = currentRage;

        NpcRageState currentState = CalculateRageState(currentRage, npc.NpcData);
        RageResult result = new RageResult(npcId, previousRage, currentRage, previousState, currentState, currentRage >= 100f);

        if (result.StateChanged)
            npc.SetRageState(currentState);

        if (result.ReachedMaxRage)
            npc.StartChase();

        if (uiManager != null)
            uiManager.SetRage(npcId, currentRage, currentState);

        UpdateScoreMultiplierFromRage();
        return result;
    }

    public RageResult ReduceRage(string npcId, float amount)
    {
        NpcController npc;
        if (!npcs.TryGetValue(npcId, out npc) || npc == null)
            return default(RageResult);

        float previousRage = GetRage(npcId);
        NpcRageState previousState = CalculateRageState(previousRage, npc.NpcData);

        float currentRage = Mathf.Clamp(previousRage - amount, 0f, 100f);
        rageByNpcId[npcId] = currentRage;

        NpcRageState currentState = CalculateRageState(currentRage, npc.NpcData);
        RageResult result = new RageResult(npcId, previousRage, currentRage, previousState, currentState, false);

        if (result.StateChanged)
            npc.SetRageState(currentState);

        if (uiManager != null)
            uiManager.SetRage(npcId, currentRage, currentState);

        UpdateScoreMultiplierFromRage();
        return result;
    }

    public void ReduceRageAround(Vector3 position, float radius, float amount)
    {
        foreach (NpcController npc in npcs.Values)
        {
            if (npc == null || !npc.CanReceiveRage)
                continue;

            if (Vector3.Distance(position, npc.transform.position) <= radius)
                ReduceRage(npc.NpcId, amount);
        }
    }

    public List<NpcController> GetAffectedNpcs(MischiefContext context)
    {
        List<NpcController> affected = new List<NpcController>();

        foreach (NpcController npc in npcs.Values)
        {
            if (npc == null || !npc.CanReceiveRage)
                continue;

            float distance = Vector3.Distance(context.Position, npc.transform.position);
            if (distance <= context.RageRadius)
                affected.Add(npc);
        }

        if (!string.IsNullOrEmpty(context.PrimaryNpcId))
        {
            NpcController primaryNpc;
            if (npcs.TryGetValue(context.PrimaryNpcId, out primaryNpc)
                && primaryNpc != null
                && primaryNpc.CanReceiveRage
                && !affected.Contains(primaryNpc))
            {
                affected.Add(primaryNpc);
            }
        }

        return affected;
    }

    private void UpdateScoreMultiplierFromRage()
    {
        if (scoreManager == null)
            return;

        scoreManager.RecalculateMultiplier(GetAverageRage());
    }

    private NpcRageState CalculateRageState(float rage, NpcData npcData)
    {
        if (npcData == null)
            return NpcRageState.Calm;

        if (rage >= npcData.enragedThreshold)
            return NpcRageState.Enraged;

        if (rage >= npcData.angryThreshold)
            return NpcRageState.Angry;

        if (rage >= npcData.annoyedThreshold)
            return NpcRageState.Annoyed;

        return NpcRageState.Calm;
    }
}
