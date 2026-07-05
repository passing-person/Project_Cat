using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ProjectCatSfxPlayer : MonoBehaviour
{
    [Header("SFX Holder")]
    [SerializeField] private ProjectCatSfxLibrary library;
    [SerializeField] private AudioSource source;
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;

    public ProjectCatSfxLibrary Library => library;

    private void Awake()
    {
        if (source == null)
        {
            source = GetComponent<AudioSource>();
        }

        if (source != null)
        {
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }
    }

    public void SetLibrary(ProjectCatSfxLibrary sfxLibrary)
    {
        library = sfxLibrary;
    }

    public bool Play(ProjectCatSfxCue cue)
    {
        if (source == null || library == null)
        {
            return false;
        }

        if (!library.TryGet(cue, out AudioClip clip, out float volume, out float pitch) || clip == null)
        {
            return false;
        }

        float previousPitch = source.pitch;
        source.pitch = pitch;
        source.PlayOneShot(clip, Mathf.Clamp01(volume * masterVolume));
        source.pitch = previousPitch;
        return true;
    }

    public bool PlaySelect() => Play(ProjectCatSfxCue.Select);
    public bool PlayMischief() => Play(ProjectCatSfxCue.Mischief);
    public bool PlayCute() => Play(ProjectCatSfxCue.Cute);
    public bool PlayHideEnter() => Play(ProjectCatSfxCue.HideEnter);
    public bool PlayHideExit() => Play(ProjectCatSfxCue.HideExit);
    public bool PlayError() => Play(ProjectCatSfxCue.Error);
    public bool PlayClear() => Play(ProjectCatSfxCue.Clear);
    public bool PlayFail() => Play(ProjectCatSfxCue.Fail);
    public bool PlayLightOn() => Play(ProjectCatSfxCue.LightOn);
    public bool PlayLightOff() => Play(ProjectCatSfxCue.LightOff);
    public bool PlayPrinterMess() => Play(ProjectCatSfxCue.PrinterMess);
    public bool PlayWaterMess() => Play(ProjectCatSfxCue.WaterMess);
    public bool PlayWorldEventComplete() => Play(ProjectCatSfxCue.WorldEventComplete);
    public bool PlayButtonUnavailable() => Play(ProjectCatSfxCue.ButtonUnavailable);
    public bool PlayMicrophoneBroadcastMeow() => Play(ProjectCatSfxCue.MicrophoneBroadcastMeow);
}
