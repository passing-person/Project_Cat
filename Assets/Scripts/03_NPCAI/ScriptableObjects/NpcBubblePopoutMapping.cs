using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/NPC/Npc Bubble Popout Mapping")]
public class NpcBubblePopoutMap : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public PopoutType type;
        public Sprite sprite;
    }

    [SerializeField] private List<Entry> entries = new();

    private Dictionary<PopoutType, Sprite> cache;

    public Sprite GetSprite(PopoutType type)
    {
        cache ??= BuildCache();
        return cache.TryGetValue(type, out var sprite) ? sprite : null;
    }

    private Dictionary<PopoutType, Sprite> BuildCache()
    {
        var dict = new Dictionary<PopoutType, Sprite>();

        foreach (var entry in entries)
        {
            if (!dict.ContainsKey(entry.type))
                dict.Add(entry.type, entry.sprite);
        }

        return dict;
    }
}

public enum PopoutType
{
    PlayerCute,
    PlayerMischief,
    Rage1,
    Rage2,
    Rage3,
    NpcCute,
    NpcConfused
}