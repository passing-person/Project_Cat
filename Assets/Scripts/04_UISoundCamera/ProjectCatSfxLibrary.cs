using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Project Cat/Audio/SFX Cue Library", fileName = "ProjectCatSfxCueLibrary")]
public class ProjectCatSfxLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ProjectCatSfxCue cue = ProjectCatSfxCue.None;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.25f, 2f)] public float pitch = 1f;
    }

    [SerializeField] private Entry[] entries = new Entry[0];

    public bool TryGet(ProjectCatSfxCue cue, out AudioClip clip, out float volume, out float pitch)
    {
        clip = null;
        volume = 1f;
        pitch = 1f;

        if (cue == ProjectCatSfxCue.None || entries == null)
        {
            return false;
        }

        for (int i = 0; i < entries.Length; i++)
        {
            Entry entry = entries[i];
            if (entry == null || entry.cue != cue || entry.clip == null)
            {
                continue;
            }

            clip = entry.clip;
            volume = Mathf.Clamp01(entry.volume);
            pitch = Mathf.Clamp(entry.pitch, 0.25f, 2f);
            return true;
        }

        return false;
    }
}
