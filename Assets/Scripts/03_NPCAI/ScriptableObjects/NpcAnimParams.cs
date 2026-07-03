using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/NPC/Npc Anim Params")]
public class NpcAnimParams : ScriptableObject
{
    [SerializeField] private List<AnimParam> animParams = new();

    private readonly Dictionary<AnimState, AnimParam> paramDict = new();

    private void OnEnable()
    {
        BuildDictionary();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        BuildDictionary();
    }
#endif

    private void BuildDictionary()
    {
        paramDict.Clear();

        foreach (AnimParam param in animParams)
        {
            if (param == null)
                continue;

            if (paramDict.ContainsKey(param.animState))
            {
                Debug.LogWarning($"Duplicate animation mapping for {param.animState} in {name}.");
                continue;
            }

            paramDict.Add(param.animState, param);
        }
    }

    public bool TryGetAnimParam(AnimState animState, out AnimParam param)
    {
        if (paramDict.Count == 0)
            BuildDictionary();

        return paramDict.TryGetValue(animState, out param);
    }

    public string GetAnimName(AnimState animState)
    {
        if (TryGetAnimParam(animState, out AnimParam param))
            return param.animatorStateName;

        Debug.LogWarning($"Animation not found for AnimState: {animState}");
        return null;
    }
}

[System.Serializable]
public class AnimParam
{
    public AnimState animState;

    [Tooltip("Gameplay state that usually owns this animation.")]
    public NpcState requiredState;

    [Tooltip("Must exactly match the Animator state name.")]
    public string animatorStateName;

    [Min(0f)]
    public float crossFadeTime = 0.1f;

    [Header("Root Motion")]
    [Tooltip("If true, Animator.applyRootMotion will be enabled before this animation plays.")]
    public bool applyRootMotion = false;

    [Tooltip("If true, NavMeshAgent transform updates are paused while this root-motion animation plays.")]
    public bool pauseAgentUpdateWhileRootMotion = true;

    [Header("IK")]
    [Tooltip("If true, IK is allowed during this animation.")]
    public bool enableAnimatorIK = false;
}

public enum AnimState
{
    Locomotion,

    Search,
    TransitionSearchToIdle,

    Dive,
    TransitionDiveToCooldown,

    ChaseCooldown,
    DiveCooldown,

    OverrideMove,

    Caught,

    TransitionStandToSit,
    IdleSitting
}
