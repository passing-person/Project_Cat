using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class NpcOverrideBehavior : MonoBehaviour
{
    [SerializeField] List<OverrideScheduleEntry> OverrideSchedule;
    
    private NpcController controller;

    private string NpcId => controller.NpcId;

    private void Awake()
    {
        LazyInstantiate();
    }

    public void Supervisor()
    {

    }

    public void Worker()
    {

    }

    public void Cleaner()
    {

    }

    public void Security()
    {

    }

    public void ExitState()
    {

    }

    public void OnMischiefWorldEvent(MischiefWorldEventContext context)
    {
        var WorldEventType = context.EventType;
        switch (WorldEventType)
        {
            case MischiefWorldEventType.None:
                Debug.LogWarning($"[NPC] {NpcId}: cannot resolve world event of type {WorldEventType}.");
                break;
            case MischiefWorldEventType.Auto:
            case MischiefWorldEventType.GenericMess:
                Debug.LogWarning($"[NPC] {NpcId}: world event of type {WorldEventType} not implemented.");
                break;
        }
    }

    // make sure non-reacter doesn't call these signal methods.
    private void CompleteMischiefWorldEvent(string targetId)
    {
        controller.CompleteMischiefWorldEvent(targetId);
    }
    private void CompleteMischiefWorldEvent(string targetId, bool value1, float value2)
    {
        controller.CompleteMischiefWorldEvent(targetId, value1, value2);
    }

    private void LazyInstantiate()
    {
        if (controller == null)
            controller = GetComponent<NpcController>();
    }

    [System.Serializable]
    private struct OverrideScheduleEntry
    {
        [SerializeField] MischiefWorldEventType OverrideType;
        [Tooltip("Smaller entries have higher priority")]
        [SerializeField] int priority;
        [Tooltip("FIFO order for events with the same level of priority. Smaller entries are handled eariler.")]
        [SerializeField] int order;
        [SerializeField] string targetId;
    }
}
