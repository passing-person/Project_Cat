using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    [Header("Libraries")]
    [SerializeField] private AudioSfxLibrary sfxLibrary;

    [Header("Fallback")]
    [SerializeField] private bool logMissingClips = true;
    [SerializeField] private bool createTemporarySourceForSpatialSfx = true;

    private readonly Dictionary<string, AudioSfxLibrary.Entry> sfxMap = new Dictionary<string, AudioSfxLibrary.Entry>();

    private void Awake()
    {
        EnsureSources();
        RebuildSfxMap();
    }

    private void OnValidate()
    {
        RebuildSfxMap();
    }

    public void SetSfxLibrary(AudioSfxLibrary library)
    {
        sfxLibrary = library;
        RebuildSfxMap();
    }

    public void PlaySfx(string sfxId)
    {
        EnsureSources();

        if (TryGetEntry(sfxId, out AudioSfxLibrary.Entry entry))
        {
            PlayEntry(entry, sfxSource, transform.position, false);
            return;
        }

        LogMissing(sfxId);
    }

    public void PlaySfxAt(string sfxId, Vector3 position)
    {
        EnsureSources();

        if (TryGetEntry(sfxId, out AudioSfxLibrary.Entry entry))
        {
            if (entry.spatial && createTemporarySourceForSpatialSfx)
            {
                PlaySpatialEntry(entry, position);
                return;
            }

            PlayEntry(entry, sfxSource, position, false);
            return;
        }

        LogMissing(sfxId);
    }

    public void PlayBgm(string bgmId)
    {
        EnsureSources();
        Debug.Log("Play BGM: " + bgmId, this);

        if (bgmSource != null && bgmSource.clip != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }

    public void StopBgm()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    private void EnsureSources()
    {
        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        if (bgmSource == null)
        {
            AudioSource[] sources = GetComponents<AudioSource>();
            if (sources.Length > 1)
            {
                bgmSource = sources[1];
            }
        }
    }

    private void RebuildSfxMap()
    {
        sfxMap.Clear();
        if (sfxLibrary == null || sfxLibrary.Entries == null)
        {
            return;
        }

        foreach (AudioSfxLibrary.Entry entry in sfxLibrary.Entries)
        {
            if (entry == null || string.IsNullOrWhiteSpace(entry.id) || entry.clip == null)
            {
                continue;
            }

            sfxMap[entry.id] = entry;
        }
    }

    private bool TryGetEntry(string sfxId, out AudioSfxLibrary.Entry entry)
    {
        entry = null;

        if (sfxMap.Count == 0)
        {
            RebuildSfxMap();
        }

        return !string.IsNullOrWhiteSpace(sfxId) && sfxMap.TryGetValue(sfxId, out entry) && entry != null;
    }

    private void PlayEntry(AudioSfxLibrary.Entry entry, AudioSource source, Vector3 position, bool forceSpatial)
    {
        if (entry == null || entry.clip == null || source == null)
        {
            return;
        }

        float previousPitch = source.pitch;
        float previousSpatialBlend = source.spatialBlend;
        source.pitch = Mathf.Max(0.01f, entry.pitch);
        source.spatialBlend = forceSpatial || entry.spatial ? 1f : 0f;
        source.transform.position = position;
        source.PlayOneShot(entry.clip, Mathf.Clamp01(entry.volume));
        source.pitch = previousPitch;
        source.spatialBlend = previousSpatialBlend;
    }

    private void PlaySpatialEntry(AudioSfxLibrary.Entry entry, Vector3 position)
    {
        GameObject temp = new GameObject("TempSFX_" + entry.id);
        temp.transform.position = position;
        AudioSource source = temp.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.pitch = Mathf.Max(0.01f, entry.pitch);
        source.PlayOneShot(entry.clip, Mathf.Clamp01(entry.volume));
        Destroy(temp, Mathf.Max(0.1f, entry.clip.length + 0.1f));
    }

    private void LogMissing(string sfxId)
    {
        if (logMissingClips)
        {
            Debug.Log("Play SFX placeholder: " + sfxId, this);
        }
    }
}
