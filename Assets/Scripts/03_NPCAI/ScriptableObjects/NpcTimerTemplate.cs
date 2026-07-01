using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/NPC/Npc Timer Template")]
public class NpcTimerTemplate : ScriptableObject
{
    public List<NpcTimerValue> timerValues;

    [System.Serializable]
    public class NpcTimerValue
    {
        public NpcTimerType timerType;
        public float duration;
        public bool isOneShot;
    }
}
