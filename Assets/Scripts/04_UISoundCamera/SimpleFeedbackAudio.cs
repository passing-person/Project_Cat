using UnityEngine;

public class SimpleFeedbackAudio : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;

    private void Awake()
    {
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
        }
    }

    public void PlaySelect() => Play("ui_select");
    public void PlayMischief() => Play("cat_mischief");
    public void PlayCute() => Play("cat_cute");
    public void PlayHideEnter() => Play("cat_hide_enter");
    public void PlayHideExit() => Play("cat_hide_exit");
    public void PlayClear() => Play("ui_clear");
    public void PlayFail() => Play("ui_fail");
    public void PlayError() => Play("ui_error");
    public void PlayWorldEvent(string eventId) => Play(eventId);

    private void Play(string sfxId)
    {
        if (audioManager == null)
        {
            audioManager = FindObjectOfType<AudioManager>();
        }

        if (audioManager != null)
        {
            audioManager.PlaySfx(sfxId);
        }
    }
}
