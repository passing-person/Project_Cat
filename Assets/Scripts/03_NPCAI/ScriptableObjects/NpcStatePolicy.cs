using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/NPC/NpcState Policy")]
public class NpcStatePolicy : ScriptableObject
{
    public List<NpcState> nonInterruptible;
}
