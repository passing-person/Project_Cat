using UnityEngine;
using UnityEngine.Events;

public class MischiefEffectPlayer : MonoBehaviour
{
    [Header("Effects")]
    [SerializeField] private ParticleSystem[] particleEffects;
    [SerializeField] private GameObject[] enableOnMischief;
    [SerializeField] private AudioSource audioSource;

    [Header("Options")]
    [SerializeField] private bool restartParticles = true;
    [SerializeField] private bool playAudio = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onMischiefEffect;

    public void Play()
    {
        SetEnabledObjects(true);
        PlayParticles();

        if (playAudio && audioSource != null)
        {
            audioSource.Play();
        }

        onMischiefEffect?.Invoke();
    }

    public void ResetEffects()
    {
        SetEnabledObjects(false);

        if (particleEffects == null)
        {
            return;
        }

        for (int i = 0; i < particleEffects.Length; i++)
        {
            ParticleSystem effect = particleEffects[i];
            if (effect == null)
            {
                continue;
            }

            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void PlayParticles()
    {
        if (particleEffects == null)
        {
            return;
        }

        for (int i = 0; i < particleEffects.Length; i++)
        {
            ParticleSystem effect = particleEffects[i];
            if (effect == null)
            {
                continue;
            }

            if (restartParticles)
            {
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            effect.Play(true);
        }
    }

    private void SetEnabledObjects(bool value)
    {
        if (enableOnMischief == null)
        {
            return;
        }

        for (int i = 0; i < enableOnMischief.Length; i++)
        {
            GameObject target = enableOnMischief[i];
            if (target != null)
            {
                target.SetActive(value);
            }
        }
    }
}
