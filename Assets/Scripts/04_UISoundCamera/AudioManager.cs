using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource bgmSource;

    public void PlaySfx(string sfxId)
    {
        // TODO: Map sfxId to an AudioClip later.
        Debug.Log("Play SFX: " + sfxId);
    }

    public void PlaySfxAt(string sfxId, Vector3 position)
    {
        // TODO: Use spatial audio or pooled audio sources later.
        Debug.Log("Play SFX at " + position + ": " + sfxId);
    }

    public void PlayBgm(string bgmId)
    {
        // TODO: Map bgmId to an AudioClip later.
        Debug.Log("Play BGM: " + bgmId);

        if (bgmSource != null && !bgmSource.isPlaying)
            bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource != null)
            bgmSource.Stop();
    }
}
