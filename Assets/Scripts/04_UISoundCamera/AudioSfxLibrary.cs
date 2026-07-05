using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/Audio/SFX Library")]
public class AudioSfxLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.25f, 3f)] public float pitch = 1f;
        public bool spatial = false;
    }

    [SerializeField] private Entry[] entries;

    public bool TryGet(string id, out Entry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(id) || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            Entry current = entries[i];
            if (current != null && current.id == id && current.clip != null)
            {
                entry = current;
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<Entry> Entries => entries;
}
