using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class RageManager : MonoBehaviour
{
    [Header("References")]
    public ScoreManager scoreManager;
    public MonoBehaviour uiBridgeBehaviour;

    private readonly Dictionary<string, IRageReceiver> receivers = new Dictionary<string, IRageReceiver>();
    private readonly Dictionary<string, float> rageValues = new Dictionary<string, float>();
    private readonly Dictionary<string, NpcRageState> rageStates = new Dictionary<string, NpcRageState>();
    private readonly List<IRageReceiver> affectedCache = new List<IRageReceiver>();

    private ICoreUIBridge uiBridge;

    private void Awake()
    {
        ResolveUIBridge();
    }

    public void RegisterNpc(IRageReceiver receiver)
    {
        if (receiver == null || string.IsNullOrWhiteSpace(receiver.NpcId))
        {
            Debug.LogWarning("RageManager.RegisterNpc failed: receiver or NpcId is invalid.");
            return;
        }

        RegisterNpcInternal(receiver.NpcId, receiver);
    }

    // Compatibility overload.
    // Existing NPC code may call RegisterNpc(NpcId, this) without implementing IRageReceiver yet.
    // If the object implements IRageReceiver, it is used directly. Otherwise, a reflection adapter is used.
    public void RegisterNpc(string npcId, object receiverObject)
    {
        if (string.IsNullOrWhiteSpace(npcId) || receiverObject == null)
        {
            Debug.LogWarning("RageManager.RegisterNpc failed: npcId or receiver is invalid.");
            return;
        }

        if (receiverObject is IRageReceiver rageReceiver)
        {
            RegisterNpcInternal(npcId, rageReceiver);
            return;
        }

        if (receiverObject is MonoBehaviour behaviour)
        {
            RegisterNpcInternal(npcId, new MonoBehaviourRageReceiverAdapter(npcId, behaviour));
            return;
        }

        Debug.LogWarning($"RageManager.RegisterNpc failed: {receiverObject.GetType().Name} is not supported.");
    }

    private void RegisterNpcInternal(string npcId, IRageReceiver receiver)
    {
        if (string.IsNullOrWhiteSpace(npcId) || receiver == null)
        {
            Debug.LogWarning("RageManager.RegisterNpc failed: npcId or receiver is invalid.");
            return;
        }

        receivers[npcId] = receiver;

        if (!rageValues.ContainsKey(npcId))
        {
            rageValues[npcId] = 0f;
        }

        if (!rageStates.ContainsKey(npcId))
        {
            rageStates[npcId] = NpcRageState.Calm;
        }

        RefreshRageUI(npcId);
        RecalculateScoreMultiplier();
    }

    public void UnregisterNpc(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return;
        }

        receivers.Remove(npcId);
        rageValues.Remove(npcId);
        rageStates.Remove(npcId);
        RecalculateScoreMultiplier();
    }

    public float GetRage(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId) || !rageValues.TryGetValue(npcId, out float value))
        {
            return 0f;
        }

        return value;
    }

    public NpcRageState GetRageState(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId) || !rageStates.TryGetValue(npcId, out NpcRageState state))
        {
            return NpcRageState.Calm;
        }

        return state;
    }

    public float GetAverageRage()
    {
        float total = 0f;
        int count = 0;

        foreach (KeyValuePair<string, IRageReceiver> pair in receivers)
        {
            IRageReceiver receiver = pair.Value;
            if (receiver == null || !receiver.CanReceiveRage)
            {
                continue;
            }

            NpcData data = receiver.NpcData;
            if (data != null && data.npcType == NpcType.Security)
            {
                continue;
            }

            total += GetRage(pair.Key);
            count++;
        }

        return count == 0 ? 0f : total / count;
    }

    public IReadOnlyList<IRageReceiver> GetAffectedNpcs(MischiefContext context)
    {
        affectedCache.Clear();
        HashSet<string> addedIds = new HashSet<string>();
        float radius = Mathf.Max(0f, context.RageRadius);

        foreach (KeyValuePair<string, IRageReceiver> pair in receivers)
        {
            IRageReceiver receiver = pair.Value;
            if (receiver == null || !receiver.CanReceiveRage)
            {
                continue;
            }

            float distance = Vector3.Distance(context.Position, receiver.Position);
            bool inRange = distance <= radius;
            bool isPrimary = !string.IsNullOrWhiteSpace(context.PrimaryNpcId) && pair.Key == context.PrimaryNpcId;

            if (inRange || isPrimary)
            {
                affectedCache.Add(receiver);
                addedIds.Add(pair.Key);
            }
        }

        if (!string.IsNullOrWhiteSpace(context.PrimaryNpcId) && !addedIds.Contains(context.PrimaryNpcId))
        {
            if (receivers.TryGetValue(context.PrimaryNpcId, out IRageReceiver primary) && primary != null && primary.CanReceiveRage)
            {
                affectedCache.Add(primary);
            }
        }

        return affectedCache;
    }

    public List<RageResult> AddRageByMischief(MischiefContext context)
    {
        IReadOnlyList<IRageReceiver> affected = GetAffectedNpcs(context);
        List<RageResult> results = new List<RageResult>();

        for (int i = 0; i < affected.Count; i++)
        {
            results.Add(AddRage(affected[i].NpcId, context.BaseRageAmount));
        }

        if (results.Count > 0)
        {
            EnsureScoringStarted();
            RecalculateScoreMultiplier();
        }

        return results;
    }

    public RageResult AddRage(string npcId, float amount)
    {
        float previousRage = GetRage(npcId);
        NpcRageState previousState = GetRageState(npcId);
        float currentRage = Mathf.Clamp(previousRage + amount, 0f, 100f);

        rageValues[npcId] = currentRage;
        NpcRageState currentState = CalculateState(npcId, currentRage);
        rageStates[npcId] = currentState;

        ApplyStateToReceiver(npcId, currentState, previousState);
        RefreshRageUI(npcId);
        EnsureScoringStarted();
        RecalculateScoreMultiplier();

        bool reachedMax = previousRage < 100f && currentRage >= 100f;
        if (reachedMax && receivers.TryGetValue(npcId, out IRageReceiver receiver) && receiver != null)
        {
            receiver.StartChase();
        }

        return new RageResult(npcId, previousRage, currentRage, previousState, currentState, reachedMax);
    }

    public RageResult ReduceRage(string npcId, float amount)
    {
        float previousRage = GetRage(npcId);
        NpcRageState previousState = GetRageState(npcId);
        float currentRage = Mathf.Clamp(previousRage - Mathf.Max(0f, amount), 0f, 100f);

        rageValues[npcId] = currentRage;
        NpcRageState currentState = CalculateState(npcId, currentRage);
        rageStates[npcId] = currentState;

        ApplyStateToReceiver(npcId, currentState, previousState);
        RefreshRageUI(npcId);
        RecalculateScoreMultiplier();

        return new RageResult(npcId, previousRage, currentRage, previousState, currentState, false);
    }

    public void SetRage(string npcId, float value)
    {
        float previousRage = GetRage(npcId);
        NpcRageState previousState = GetRageState(npcId);
        float currentRage = Mathf.Clamp(value, 0f, 100f);

        rageValues[npcId] = currentRage;
        NpcRageState currentState = CalculateState(npcId, currentRage);
        rageStates[npcId] = currentState;

        ApplyStateToReceiver(npcId, currentState, previousState);
        RefreshRageUI(npcId);
        RecalculateScoreMultiplier();

        if (previousRage < 100f && currentRage >= 100f && receivers.TryGetValue(npcId, out IRageReceiver receiver) && receiver != null)
        {
            receiver.StartChase();
        }
    }

    public void ResetRage(string npcId)
    {
        SetRage(npcId, 0f);
    }

    public List<RageResult> ReduceRageAround(Vector3 position, float radius, float amount)
    {
        List<RageResult> results = new List<RageResult>();
        float safeRadius = Mathf.Max(0f, radius);

        foreach (KeyValuePair<string, IRageReceiver> pair in receivers)
        {
            IRageReceiver receiver = pair.Value;
            if (receiver == null || !receiver.CanReceiveRage)
            {
                continue;
            }

            if (Vector3.Distance(position, receiver.Position) <= safeRadius)
            {
                results.Add(ReduceRage(pair.Key, amount));
            }
        }

        return results;
    }

    public void ResetAllRage()
    {
        List<string> npcIds = new List<string>(rageValues.Keys);
        for (int i = 0; i < npcIds.Count; i++)
        {
            SetRage(npcIds[i], 0f);
        }

        RecalculateScoreMultiplier();
    }

    public void SetSecurityMultiplierOverride(bool enabled, float multiplier)
    {
        if (scoreManager == null)
        {
            return;
        }

        scoreManager.SetMultiplierOverride(multiplier, enabled);

        if (!enabled)
        {
            RecalculateScoreMultiplier();
        }
    }

    public int GetRegisteredNpcCount()
    {
        return receivers.Count;
    }

    public void SetUIBridge(ICoreUIBridge bridge)
    {
        uiBridge = bridge;
    }

    private void EnsureScoringStarted()
    {
        if (scoreManager == null)
        {
            return;
        }

        if (GetAverageRage() > 0f)
        {
            scoreManager.StartScoring();
        }
    }

    private void RecalculateScoreMultiplier()
    {
        if (scoreManager != null)
        {
            scoreManager.RecalculateMultiplier(GetAverageRage());
        }
    }

    private NpcRageState CalculateState(string npcId, float rage)
    {
        NpcData data = null;
        if (receivers.TryGetValue(npcId, out IRageReceiver receiver) && receiver != null)
        {
            data = receiver.NpcData;
        }

        float annoyed = data != null ? data.annoyedThreshold : 40f;
        float angry = data != null ? data.angryThreshold : 70f;
        float enraged = data != null ? data.enragedThreshold : 100f;

        if (rage >= enraged)
        {
            return NpcRageState.Enraged;
        }

        if (rage >= angry)
        {
            return NpcRageState.Angry;
        }

        if (rage >= annoyed)
        {
            return NpcRageState.Annoyed;
        }

        return NpcRageState.Calm;
    }

    private void ApplyStateToReceiver(string npcId, NpcRageState currentState, NpcRageState previousState)
    {
        if (currentState == previousState)
        {
            return;
        }

        if (receivers.TryGetValue(npcId, out IRageReceiver receiver) && receiver != null)
        {
            receiver.SetRageState(currentState);
        }
    }

    private void RefreshRageUI(string npcId)
    {
        if (uiBridge == null)
        {
            ResolveUIBridge();
        }

        if (uiBridge != null)
        {
            uiBridge.SetRage(npcId, GetRage(npcId), GetRageState(npcId));
        }
    }

    private void ResolveUIBridge()
    {
        uiBridge = uiBridgeBehaviour as ICoreUIBridge;
    }

    private sealed class MonoBehaviourRageReceiverAdapter : IRageReceiver
    {
        private readonly string fallbackNpcId;
        private readonly MonoBehaviour behaviour;
        private readonly Type behaviourType;

        public MonoBehaviourRageReceiverAdapter(string npcId, MonoBehaviour behaviour)
        {
            fallbackNpcId = npcId;
            this.behaviour = behaviour;
            behaviourType = behaviour != null ? behaviour.GetType() : null;
        }

        public string NpcId => ReadProperty<string>("NpcId", fallbackNpcId);
        public NpcData NpcData => ReadProperty<NpcData>("NpcData", null);
        public bool CanReceiveRage => ReadProperty<bool>("CanReceiveRage", true);
        public Vector3 Position => behaviour != null ? behaviour.transform.position : Vector3.zero;

        public void SetRageState(NpcRageState state)
        {
            Invoke("SetRageState", state);
        }

        public void StartChase()
        {
            Invoke("StartChase");
        }

        public void StopChase()
        {
            Invoke("StopChase");
        }

        public void LoseTarget()
        {
            Invoke("LoseTarget");
        }

        private T ReadProperty<T>(string propertyName, T fallback)
        {
            if (behaviour == null || behaviourType == null)
            {
                return fallback;
            }

            PropertyInfo property = behaviourType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && typeof(T).IsAssignableFrom(property.PropertyType))
            {
                object value = property.GetValue(behaviour, null);
                return value is T typedValue ? typedValue : fallback;
            }

            FieldInfo field = behaviourType.GetField(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && typeof(T).IsAssignableFrom(field.FieldType))
            {
                object value = field.GetValue(behaviour);
                return value is T typedValue ? typedValue : fallback;
            }

            return fallback;
        }

        private void Invoke(string methodName)
        {
            if (behaviour == null || behaviourType == null)
            {
                return;
            }

            MethodInfo method = behaviourType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            method?.Invoke(behaviour, null);
        }

        private void Invoke<T>(string methodName, T argument)
        {
            if (behaviour == null || behaviourType == null)
            {
                return;
            }

            MethodInfo method = behaviourType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(T) }, null);
            method?.Invoke(behaviour, new object[] { argument });
        }
    }
}
