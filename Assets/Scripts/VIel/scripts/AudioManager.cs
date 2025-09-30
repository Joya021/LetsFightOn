using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource walkingSource;

    [Header("UI Sound Effects")]
    public AudioClip correctAnswerClip;
    public AudioClip wrongAnswerClip;
    public AudioClip codeInterruptedClip;
    public AudioClip intercomInteractClip;

    [Header("Movement Sound Effects")]
    public AudioClip survivorWalkingClip;
    public AudioClip hunterWalkingClip;

    [Header("Hunter Sound Effects")]
    public AudioClip hunterInterruptCodeClip;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float sfxVolume = 1f;
    [Range(0f, 1f)]
    public float walkingVolume = 0.7f;

    public static AudioManager Instance;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

       
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        if (walkingSource == null)
        {
            walkingSource = gameObject.AddComponent<AudioSource>();
            walkingSource.loop = true;
        }

       
    }

    #region UI Sound Effects

    public void PlayCorrectAnswer()
    {
        PlaySFX(correctAnswerClip);
    }

    public void PlayWrongAnswer()
    {
        PlaySFX(wrongAnswerClip);
    }

    public void PlayCodeInterrupted()
    {
        PlaySFX(codeInterruptedClip);
    }

    public void PlayIntercomInteract()
    {
        PlaySFX(intercomInteractClip);
    }

    public void PlayHunterInterruptCode()
    {
        PlaySFX(hunterInterruptCodeClip);
    }

    #endregion

    #region Movement Sound Effects

    public void StartSurvivorWalking()
    {
        StartWalking(survivorWalkingClip);
    }

    public void StartHunterWalking()
    {
        StartWalking(hunterWalkingClip);
    }

    public void StopWalking()
    {
        if (walkingSource.isPlaying)
        {
            walkingSource.Stop();
        }
    }

    private void StartWalking(AudioClip walkClip)
    {
        if (walkClip == null) return;

        if (!walkingSource.isPlaying || walkingSource.clip != walkClip)
        {
            walkingSource.clip = walkClip;
            walkingSource.Play();
        }
    }

    #endregion

    #region Helper Methods

    private void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
            sfxSource.volume = sfxVolume;
    }

    public void SetWalkingVolume(float volume)
    {
        walkingVolume = Mathf.Clamp01(volume);
        if (walkingSource != null)
            walkingSource.volume = walkingVolume;
    }

    #endregion
}