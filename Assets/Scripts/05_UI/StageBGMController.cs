using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class StageBGMController : MonoBehaviour
{
    [Header("References")]
    public ScoreManager scoreManager;

    [Header("BGM")]
    public AudioClip calmBGM;
    public AudioClip intenseBGM;

    private AudioSource audioSource;

    private bool hasSwitched = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // 自动寻找 ScoreManager
        if (scoreManager == null)
        {
            scoreManager = FindObjectOfType<ScoreManager>();
        }

        // 播放初始BGM
        if (calmBGM != null)
        {
            audioSource.clip = calmBGM;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    void Update()
    {
        if (hasSwitched)
            return;

        if (scoreManager == null)
            return;

        if (scoreManager.HasReachedTargetScore())
        {
            SwitchToIntenseBGM();
        }
    }

    void SwitchToIntenseBGM()
    {
        hasSwitched = true;

        if (intenseBGM == null)
            return;

        audioSource.Stop();
        audioSource.clip = intenseBGM;
        audioSource.loop = true;
        audioSource.Play();

        Debug.Log("Target score reached. Switch to intense BGM.");
    }
}