using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider sfxSlider;   // Slider for SFX volume
    public Slider musicSlider; // Slider for music volume

    [Header("Audio Sources")]
    public AudioSource sfxSource;  // SFX AudioSource (for sound effects)
    public AudioSource musicSource; // Music AudioSource (for background music)

    // SFX and Music volume variables
    public float sfxVolume = 1f;
    public float musicVolume = 1f;

    void Start()
    {
        // Initialize sliders with the current volume settings
        sfxSlider.value = sfxVolume;
        musicSlider.value = musicVolume;

        // Add listeners to sliders
        sfxSlider.onValueChanged.AddListener(UpdateSFXVolume);
        musicSlider.onValueChanged.AddListener(UpdateMusicVolume);

        // Set initial volumes
        SetSFXVolume(sfxVolume);
        SetMusicVolume(musicVolume);
    }

    // Method to update SFX volume
    private void UpdateSFXVolume(float volume)
    {
        SetSFXVolume(volume);
    }

    // Method to update Music volume
    private void UpdateMusicVolume(float volume)
    {
        SetMusicVolume(volume);
    }

    // Method to set the SFX volume
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }
    }

    // Method to set the Music volume
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }
}
