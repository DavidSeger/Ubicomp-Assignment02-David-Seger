using UnityEngine;

public class MusicController : MonoBehaviour
{
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        AudioClip clip = Resources.Load<AudioClip>("Searching");
        if (clip != null)
        {
            audioSource.clip = clip;
            audioSource.loop = true;
        }
        else
        {
            Debug.LogError("Audio clip 'searching.wav' not found in Resources folder.");
        }
    }

    public void ActivateMusic()
    {
        if (audioSource != null && audioSource.clip != null)
        {
            audioSource.Play();
            Debug.Log("Music activated.");
        }
    }

    public void DeactivateMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            Debug.Log("Music deactivated.");
        }
    }
}
