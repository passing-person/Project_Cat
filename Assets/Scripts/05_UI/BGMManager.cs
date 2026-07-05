using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance;

    void Awake()
    {
        //如果已经有一个BGM，就把新的删掉
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        //切场景不会销毁
        DontDestroyOnLoad(gameObject);
    }

    public void RestartMusic()
    {
        AudioSource audio = GetComponent<AudioSource>();

        audio.Stop();
        audio.Play();
    }

    public void DestroyMusic()
    {
        Destroy(gameObject);
        Instance = null;
    }
}