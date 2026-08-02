using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcSfxPlayer : MonoBehaviour
{
    private AudioManager audioManager;

    private void Awake()
    {
        audioManager = FindObjectOfType<AudioManager>();
    }

    public void PlaySfx(string sfxId)
    {
        if (audioManager == null)
        {
            ReportIfAudioManagerMissing();
            return;
        }

        audioManager.PlaySfx(sfxId);
    }

    private void ReportIfAudioManagerMissing()
    {
            Debug.Log($"[NPC SFX] {name} cannot find audio manager in scene. " +
                $"Please ensure an AudioManager is present in the scene.");
    }
}
