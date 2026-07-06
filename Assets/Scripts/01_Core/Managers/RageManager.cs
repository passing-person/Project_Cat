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
    private readonly Dictionary<IRageReceiver, string> runtimeNpcIdsByReceiver = new Dictionary<IRageReceiver, string>();
    private readonly Dictionary<string, int> runtimeNpcIdCounters = new Dictionary<string, int>();

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

        string runtimeNpcId = GetOrCreateRuntimeNpcId(npcId, receiver);
        receivers[runtimeNpcId] = receiver;

        if (!rageValues.ContainsKey(runtimeNpcId))
        {
            rageValues[runtimeNpcId] = 0f;
        }

        if (!rageStates.ContainsKey(runtimeNpcId))
        {
            rageStates[runtimeNpcId] = NpcRageState.Calm;
        }

        RefreshRageUI(runtimeNpcId);
        RecalculateScoreMultiplier();
    }

    public void UnregisterNpc(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return;
        }

        string runtimeNpcId = ResolveNpcId(npcId);
        if (receivers.TryGetValue(runtimeNpcId, out IRageReceiver receiver) && receiver != null)
        {
            runtimeNpcIdsByReceiver.Remove(receiver);
        }

        receivers.Remove(runtimeNpcId);
        rageValues.Remove(runtimeNpcId);
        rageStates.Remove(runtimeNpcId);
        RecalculateScoreMultiplier();
    }

    public float GetRage(string npcId)
    {
        string runtimeNpcId = ResolveNpcId(npcId);
        if (string.IsNullOrWhiteSpace(runtimeNpcId) || !rageValues.TryGetValue(runtimeNpcId, out float value))
        {
            return 0f;
        }

        return value;
    }

    public NpcRageState GetRageState(string npcId)
    {
        string runtimeNpcId = ResolveNpcId(npcId);
        if (string.IsNullOrWhiteSpace(runtimeNpcId) || !rageStates.TryGetValue(runtimeNpcId, out NpcRageState state))
        {
            return NpcRageState.Calm;
        }

        return state;
    }

    public List<string> GetRegisteredNpcIds()
    {
        return new List<string>(receivers.Keys);
    }

    public bool TryGetNpcPosition(string npcId, out Vector3 position)
    {
        position = Vector3.zero;

        string runtimeNpcId = ResolveNpcId(npcId);
        if (string.IsNullOrWhiteSpace(runtimeNpcId) || !receivers.TryGetValue(runtimeNpcId, out IRageReceiver receiver) || receiver == null)
        {
            return false;
        }

        position = receiver.Position;
        return true;
    }

    public bool TryGetRuntimeNpcId(IRageReceiver receiver, out string runtimeNpcId)
    {
        runtimeNpcId = string.Empty;
        if (receiver == null)
        {
            return false;
        }

        if (runtimeNpcIdsByReceiver.TryGetValue(receiver, out runtimeNpcId) && !string.IsNullOrWhiteSpace(runtimeNpcId))
        {
            return true;
        }

        foreach (KeyValuePair<string, IRageReceiver> pair in receivers)
        {
            if (ReferenceEquals(pair.Value, receiver))
            {
                runtimeNpcId = pair.Key;
                runtimeNpcIdsByReceiver[receiver] = runtimeNpcId;
                return true;
            }
        }

        return false;
    }

    public string GetRuntimeNpcId(IRageReceiver receiver)
    {
        return TryGetRuntimeNpcId(receiver, out string runtimeNpcId) ? runtimeNpcId : string.Empty;
    }

    public bool TryGetReceiver(string npcId, out IRageReceiver receiver)
    {
        receiver = null;

        string runtimeNpcId = ResolveNpcId(npcId);
        if (string.IsNullOrWhiteSpace(runtimeNpcId))
        {
            return false;
        }

        return receivers.TryGetValue(runtimeNpcId, out receiver) && receiver != null;
    }

    public bool TryFindNearestNpc(Vector3 position, out string npcId, out IRageReceiver receiver, bool excludeSecurity = true)
    {
        npcId = string.Empty;
        receiver = null;
        float bestDistanceSqr = float.PositiveInfinity;

        foreach (KeyValuePair<string, IRageReceiver> pair in receivers)
        {
            IRageReceiver candidate = pair.Value;
            if (candidate == null || !candidate.CanReceiveRage)
            {
                continue;
            }

            NpcData data = candidate.NpcData;
            if (excludeSecurity && data != null && data.npcType == NpcType.Security)
            {
                continue;
            }

            float distanceSqr = (candidate.Position - position).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                npcId = pair.Key;
                receiver = candidate;
            }
        }

        return receiver != null;
    }

    public bool TryFindNearestNpcOfType(NpcType npcType, Vector3 position, out string npcId, out IRageReceiver receiver)
    {
        npcId = string.Empty;
        receiver = null;
        float bestDistanceSqr = float.PositiveInfinity;

        foreach (KeyValuePair<string, IRageReceiver> pair in receivers)
        {
            IRageReceiver candidate = pair.Value;
            if (candidate == null || !candidate.CanReceiveRage)
            {
                continue;
            }

            NpcData data = candidate.NpcData;
            if (data == null || data.npcType != npcType)
            {
                continue;
            }

            float distanceSqr = (candidate.Position - position).sqrMagnitude;
            if (distanceSqr < bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                npcId = pair.Key;
                receiver = candidate;
            }
        }

        return receiver != null;
    }

    private string GetOrCreateRuntimeNpcId(string baseNpcId, IRageReceiver receiver)
    {
        if (runtimeNpcIdsByReceiver.TryGetValue(receiver, out string existingRuntimeId) && !string.IsNullOrWhiteSpace(existingRuntimeId))
        {
            return existingRuntimeId;
        }

        string safeBaseId = string.IsNullOrWhiteSpace(baseNpcId) ? "NPC" : baseNpcId.Trim();
        if (!receivers.TryGetValue(safeBaseId, out IRageReceiver existingReceiver) || ReferenceEquals(existingReceiver, receiver))
        {
            runtimeNpcIdsByReceiver[receiver] = safeBaseId;
            return safeBaseId;
        }

        int nextIndex = runtimeNpcIdCounters.TryGetValue(safeBaseId, out int currentIndex) ? currentIndex + 1 : 2;
        string candidate = safeBaseId + "_" + nextIndex;
        while (receivers.ContainsKey(candidate))
        {
            nextIndex++;
            candidate = safeBaseId + "_" + nextIndex;
        }

        runtimeNpcIdCounters[safeBaseId] = nextIndex;
        runtimeNpcIdsByReceiver[receiver] = candidate;
        return candidate;
    }

    private string ResolveNpcId(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId))
        {
            return string.Empty;
        }

        if (receivers.ContainsKey(npcId) || rageValues.ContainsKey(npcId))
        {
            return npcId;
        }

        foreach (KeyValuePair<string, IRageReceiver> pair in receivers)
        {
            IRageReceiver receiver = pair.Value;
            if (receiver != null && receiver.NpcId == npcId)
            {
                return pair.Key;
            }
        }

        return npcId;
    }

    private string GetRuntimeNpcIdOrFallback(IRageReceiver receiver)
    {
        if (TryGetRuntimeNpcId(receiver, out string runtimeNpcId))
        {
            return runtimeNpcId;
        }

        return receiver != null ? ResolveNpcId(receiver.NpcId) : string.Empty;
    }

    public float GetEnragedThreshold(string npcId)
    {
        if (!string.IsNullOrWhiteSpace(npcId) && receivers.TryGetValue(npcId, out IRageReceiver receiver) && receiver != null && receiver.NpcData != null)
        {
            return receiver.NpcData.enragedThreshold;
        }

        return 100f;
    }

    public float GetCuteActionRadiusHint()
    {
        return 5f;
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
            string runtimeNpcId = GetRuntimeNpcIdOrFallback(affected[i]);
            results.Add(AddRage(runtimeNpcId, context.BaseRageAmount));
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
        npcId = ResolveNpcId(npcId);
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

        float enragedThreshold = GetEnragedThreshold(npcId);
        bool reachedMax = previousRage < enragedThreshold && currentRage >= enragedThreshold;
        if (reachedMax && receivers.TryGetValue(npcId, out IRageReceiver receiver) && receiver != null)
        {
            receiver.StartChase();
        }

        return new RageResult(npcId, previousRage, currentRage, previousState, currentState, reachedMax);
    }

    public RageResult ReduceRage(string npcId, float amount)
    {
        npcId = ResolveNpcId(npcId);
        float previousRage = GetRage(npcId);
        NpcRageState previousState = GetRageState(npcId);
        float currentRage = Mathf.Clamp(previousRage - Mathf.Max(0f, amount), 0f, 100f);

        rageValues[npcId] = currentRage;
        NpcRageState currentState = CalculateState(npcId, currentRage);
        rageStates[npcId] = currentState;

        ApplyStateToReceiver(npcId, currentState, previousState);
        ApplyChaseStopIfRageDropped(npcId, previousRage, currentRage);
        RefreshRageUI(npcId);
        RecalculateScoreMultiplier();

        return new RageResult(npcId, previousRage, currentRage, previousState, currentState, false);
    }

    public void SetRage(string npcId, float value)
    {
        npcId = ResolveNpcId(npcId);
        float previousRage = GetRage(npcId);
        NpcRageState previousState = GetRageState(npcId);
        float currentRage = Mathf.Clamp(value, 0f, 100f);

        rageValues[npcId] = currentRage;
        NpcRageState currentState = CalculateState(npcId, currentRage);
        rageStates[npcId] = currentState;

        ApplyStateToReceiver(npcId, currentState, previousState);
        ApplyChaseStopIfRageDropped(npcId, previousRage, currentRage);
        RefreshRageUI(npcId);
        RecalculateScoreMultiplier();

        if (previousRage < GetEnragedThreshold(npcId) && currentRage >= GetEnragedThreshold(npcId) && receivers.TryGetValue(npcId, out IRageReceiver receiver) && receiver != null)
        {
            receiver.StartChase();
        }
    }

    public void ResetRage(string npcId)
    {
        SetRage(npcId, 0f);
    }

    public List<RageResult> ReduceRageAround(Vector3 position, float radius, float amount, bool excludeSecurity = false)
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

            if (excludeSecurity)
            {
                NpcData data = receiver.NpcData;
                if (data != null && data.npcType == NpcType.Security)
                {
                    continue;
                }
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

    private void ApplyChaseStopIfRageDropped(string npcId, float previousRage, float currentRage)
    {
        float enragedThreshold = GetEnragedThreshold(npcId);
        if (previousRage >= enragedThreshold && currentRage < enragedThreshold)
        {
            if (receivers.TryGetValue(npcId, out IRageReceiver receiver) && receiver != null)
            {
                receiver.StopChase();
            }
        }
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

    private sealed class MonoBehaviourRageReceiverAdapter : IRageReceiver, IMischiefWorldEventReceiver
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


        public void OnMischiefWorldEvent(MischiefWorldEventContext context)
        {
            if (Invoke("OnMischiefWorldEvent", context)) return;
            if (Invoke("OnMischiefEvent", context)) return;
            if (Invoke("HandleMischiefWorldEvent", context)) return;

            // Compatibility fallback methods do not receive shouldReact.
            // Only call them for the unique reactor to avoid making every NPC perform the main reaction.
            if (!context.ShouldReact)
            {
                return;
            }

            if (context.EventType == MischiefWorldEventType.LightToggle)
            {
                if (Invoke("OnLightEvent", context.TargetId, context.Position)) return;
                Invoke("OnLightEvent", context.TargetId, context.Position, context.EventType);
                return;
            }

            if (context.EventType == MischiefWorldEventType.PrinterMess
                || context.EventType == MischiefWorldEventType.WaterDispenserMess
                || context.EventType == MischiefWorldEventType.GenericMess)
            {
                if (Invoke("OnMessEvent", context.TargetId, context.Position, context.EventType)) return;
                Invoke("OnMessEvent", context.TargetId, context.Position);
            }
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

        private bool Invoke(string methodName)
        {
            if (behaviour == null || behaviourType == null)
            {
                return false;
            }

            MethodInfo method = behaviourType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (method == null)
            {
                return false;
            }

            method.Invoke(behaviour, null);
            return true;
        }

        private bool Invoke<T>(string methodName, T argument)
        {
            if (behaviour == null || behaviourType == null)
            {
                return false;
            }

            MethodInfo method = behaviourType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(T) }, null);
            if (method == null)
            {
                return false;
            }

            method.Invoke(behaviour, new object[] { argument });
            return true;
        }

        private bool Invoke<T1, T2>(string methodName, T1 argument1, T2 argument2)
        {
            if (behaviour == null || behaviourType == null)
            {
                return false;
            }

            MethodInfo method = behaviourType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(T1), typeof(T2) }, null);
            if (method == null)
            {
                return false;
            }

            method.Invoke(behaviour, new object[] { argument1, argument2 });
            return true;
        }

        private bool Invoke<T1, T2, T3>(string methodName, T1 argument1, T2 argument2, T3 argument3)
        {
            if (behaviour == null || behaviourType == null)
            {
                return false;
            }

            MethodInfo method = behaviourType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new[] { typeof(T1), typeof(T2), typeof(T3) }, null);
            if (method == null)
            {
                return false;
            }

            method.Invoke(behaviour, new object[] { argument1, argument2, argument3 });
            return true;
        }
    }
}
