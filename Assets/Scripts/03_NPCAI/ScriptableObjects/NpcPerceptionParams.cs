using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/NPC/View Params", fileName = "Default")]
public class NpcViewParams : ScriptableObject
{
    public float sectorRadius = 3.5f;
    public float sectorHeight = 1.5f;
    [Tooltip("Measured in deg.")]
    public float sectorDeg = 140f;
}
