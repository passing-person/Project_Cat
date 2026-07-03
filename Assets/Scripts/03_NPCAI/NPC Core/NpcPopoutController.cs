using UnityEngine;

public class NpcPopoutController : MonoBehaviour
{
    [SerializeField] private BubblePopoutLogic bubblePopout;
    [SerializeField] private NpcAnimationMachine animationMachine;
    [SerializeField] private float defaultDuration = 2f;

    private NpcState cachedState;
    private NpcRageState cachedRage = NpcRageState.Calm;
    private bool hasCache;

    public void ShowPopout(PopoutType type,
    float duration,
    NpcPopoutAnchorMode anchorMode)
    {
        bubblePopout.Show(type, duration, anchorMode);
    }

    public void UpdatePopout(NpcStateSnapshot snapshot)
    {
        var state = snapshot.currentState;
        var rage = snapshot.currentRageState;

        bool stateChanged = !hasCache || state != cachedState;
        bool rageChanged = !hasCache || rage != cachedRage;
        bool rageReduced = hasCache && rage < cachedRage;

        if (!stateChanged && !rageChanged)
            return;

        if (ResolvePopoutType(state, rage, rageReduced, out PopoutType type))
        {
            NpcPopoutAnchorMode anchorMode = ResolveCurrentAnchorMode();

            if (anchorMode != NpcPopoutAnchorMode.None)
            {
                ShowPopout(type, defaultDuration, anchorMode);
                Debug.Log($"[NPC Popout] {name}: Popout of type {type}, anchorMode {anchorMode}.");
            }
        }

        cachedState = state;
        cachedRage = rage;
        hasCache = true;
    }

    public bool ResolvePopoutType(NpcState state, NpcRageState rage, bool rageReduced, out PopoutType type)
    {
        if (rageReduced)
        {
            type = PopoutType.NpcCute; // only cute action can reduce rage
            return true;
        }

        if (state == NpcState.Search)
        {
            type = PopoutType.NpcConfused;
            return true;
        }

        switch (rage)
        {
            case NpcRageState.Annoyed:
                type = PopoutType.Rage1;
                return true;
            case NpcRageState.Angry:
                type = PopoutType.Rage2;
                return true;
            case NpcRageState.Enraged:
                type = PopoutType.Rage3;
                return true;
            default:
                type = PopoutType.Rage1; // fallback
                return false;
        }
    }

    private NpcPopoutAnchorMode ResolveCurrentAnchorMode()
    {
        if (animationMachine != null &&
            animationMachine.TryGetCurrentPopoutAnchorMode(out var mode))
        {
            return mode;
        }

        return NpcPopoutAnchorMode.Standing;
    }
}