using UnityEngine;
using UnityEngine.UI;

public class SoundManager : MonoBehaviour
{
    [Header("UI Sliders")]
    public Slider sfxSlider;
    public Slider musicSlider;
    public Slider voicelineSlider; // NEW: Voiceline volume slider

    [Header("UI Toggles")]
    public Toggle voicelineToggle; // NEW: Toggle for enabling/disabling voicelines

    [Header("Audio Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    // SFX and Music volume variables
    public float sfxVolume = 1f;
    public float musicVolume = 1f;
    public float voicelineVolume = 0.8f; // NEW: Voiceline volume

    // NEW: Voiceline enabled state
    public bool voicelinesEnabled = true;

    // Singleton pattern
    public static SoundManager Instance;

    void Awake()
    {
        // Singleton setup
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
    }

    void Start()
    {
        // Initialize sliders with the current volume settings
        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVolume;
            sfxSlider.onValueChanged.AddListener(UpdateSFXVolume);
        }

        if (musicSlider != null)
        {
            musicSlider.value = musicVolume;
            musicSlider.onValueChanged.AddListener(UpdateMusicVolume);
        }

        // NEW: Initialize voiceline slider
        if (voicelineSlider != null)
        {
            voicelineSlider.value = voicelineVolume;
            voicelineSlider.onValueChanged.AddListener(UpdateVoicelineVolume);
        }

        // NEW: Initialize voiceline toggle
        if (voicelineToggle != null)
        {
            voicelineToggle.isOn = voicelinesEnabled;
            voicelineToggle.onValueChanged.AddListener(ToggleVoicelines);
        }

        // Set initial volumes
        SetSFXVolume(sfxVolume);
        SetMusicVolume(musicVolume);
        SetVoicelineVolume(voicelineVolume);
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

    // NEW: Method to update Voiceline volume
    private void UpdateVoicelineVolume(float volume)
    {
        SetVoicelineVolume(volume);
    }

    // NEW: Method to toggle voicelines on/off
    private void ToggleVoicelines(bool enabled)
    {
        voicelinesEnabled = enabled;
        Debug.Log($"[SoundManager] Voicelines {(enabled ? "enabled" : "disabled")}");

        // Update all active VoicelinePlayer components
        VoicelinePlayer[] voicelinePlayers = FindObjectsOfType<VoicelinePlayer>();
        foreach (VoicelinePlayer player in voicelinePlayers)
        {
            if (enabled)
            {
                player.StartVoicelines();
            }
            else
            {
                player.StopVoicelines();
            }
        }
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

    // NEW: Method to set the Voiceline volume
    public void SetVoicelineVolume(float volume)
    {
        voicelineVolume = Mathf.Clamp01(volume);

        // Update all active VoicelinePlayer components
        VoicelinePlayer[] voicelinePlayers = FindObjectsOfType<VoicelinePlayer>();
        foreach (VoicelinePlayer player in voicelinePlayers)
        {
            player.UpdateVolume(voicelineVolume);
        }
    }
}