using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class SimpleFeedbackAudio : MonoBehaviour
{
    [Header("Runtime Feedback")]
    [SerializeField] private bool enableGeneratedSfx = true;
    [SerializeField, Range(0f, 1f)] private float volume = 0.35f;

    private AudioSource source;
    private readonly Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
    }

    public void PlaySelect()
    {
        PlayTone("select", 720f, 0.055f, volume * 0.65f);
    }

    public void PlayMischief()
    {
        PlayTone("mischief", 420f, 0.11f, volume);
        PlayTone("mischief_high", 880f, 0.07f, volume * 0.55f);
    }

    public void PlayHideEnter()
    {
        PlayTone("hide_enter", 260f, 0.14f, volume * 0.85f);
    }

    public void PlayHideExit()
    {
        PlayTone("hide_exit", 520f, 0.08f, volume * 0.7f);
    }

    public void PlayCute()
    {
        PlayTone("cute_a", 760f, 0.075f, volume * 0.8f);
        PlayTone("cute_b", 1140f, 0.09f, volume * 0.55f);
    }

    public void PlayError()
    {
        PlayTone("error", 160f, 0.12f, volume * 0.85f);
    }

    public void PlayClear()
    {
        PlayTone("clear_a", 660f, 0.12f, volume);
        PlayTone("clear_b", 990f, 0.16f, volume * 0.8f);
    }

    public void PlayFail()
    {
        PlayTone("fail", 140f, 0.25f, volume);
    }

    private void PlayTone(string key, float frequency, float duration, float targetVolume)
    {
        if (!enableGeneratedSfx || source == null)
        {
            return;
        }

        AudioClip clip = GetOrCreateTone(key, frequency, duration);
        source.PlayOneShot(clip, Mathf.Clamp01(targetVolume));
    }

    private AudioClip GetOrCreateTone(string key, float frequency, float duration)
    {
        if (clipCache.TryGetValue(key, out AudioClip cached) && cached != null)
        {
            return cached;
        }

        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - ((float)i / sampleCount);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope;
        }

        AudioClip clip = AudioClip.Create("RuntimeSfx_" + key, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        clipCache[key] = clip;
        return clip;
    }
}
