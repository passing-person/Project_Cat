using UnityEngine;

public class ObjectAudioController : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip interactClip;

    public void PlayInteractSound()
    {
        if (audioSource != null && interactClip != null)
            audioSource.PlayOneShot(interactClip);
    }
}
