using UnityEngine;

public class PlayerSfxController : MonoBehaviour
{
    [SerializeField] private AudioManager audioManager;

    public void PlayJump()
    {
        Play("cat_jump");
    }

    public void PlayMischief()
    {
        Play("cat_mischief");
    }

    public void PlayCute()
    {
        Play("cat_cute");
    }

    public void PlayHideEnter()
    {
        Play("cat_hide_enter");
    }

    public void PlayHideExit()
    {
        Play("cat_hide_exit");
    }

    public void PlayCaught()
    {
        Play("cat_caught");
    }

    public void PlayMeow()
    {
        Play("cat_meow");
    }

    private void Play(string sfxId)
    {
        if (audioManager == null)
            return;

        audioManager.PlaySfxAt(sfxId, transform.position);
    }
}
