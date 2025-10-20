using UnityEngine;
using System.Collections;
using Photon.Pun;

public class VoicelinePlayer : MonoBehaviour
{
    [Header("Voiceline Settings")]
    [Tooltip("Array of 3 voiceline audio clips for this character")]
    public AudioClip[] voicelineClips = new AudioClip[3];

    [Header("Timing Settings")]
    [Tooltip("Minimum time (seconds) between voicelines")]
    public float minTimeBetweenVoicelines = 10f;

    [Tooltip("Maximum time (seconds) between voicelines")]
    public float maxTimeBetweenVoicelines = 30f;

    [Tooltip("Delay before first voiceline plays")]
    public float initialDelay = 3f;

    [Header("Audio Source")]
    [Tooltip("Dedicated audio source for voicelines")]
    public AudioSource voicelineAudioSource;

    [Header("Volume Settings")]
    [Range(0f, 1f)]
    public float voicelineVolume = 0.8f;

    // Private variables
    private bool isPlaying = false;
    private bool gameEnded = false;
    private bool isDead = false;
    private Coroutine voicelineCoroutine;
    private PhotonView photonView;
    private bool isLocalPlayer = false;

    void Start()
    {
        // CRITICAL: Check if this is the local player
        photonView = GetComponent<PhotonView>();

        // Determine if this is the local player
        if (photonView != null)
        {
            // Multiplayer mode
            isLocalPlayer = photonView.IsMine;
            Debug.Log($"[VoicelinePlayer] {gameObject.name} - Multiplayer Mode, IsMine: {isLocalPlayer}");
        }
        else
        {
            // Offline/Single-player mode - always play voicelines
            isLocalPlayer = true;
            Debug.Log($"[VoicelinePlayer] {gameObject.name} - Offline Mode, treating as local player");
        }

        // ONLY setup audio for the local player
        if (!isLocalPlayer)
        {
            Debug.Log($"[VoicelinePlayer] {gameObject.name} is NOT local player - voicelines DISABLED");
            enabled = false; // Disable this script component
            return;
        }

        Debug.Log($"[VoicelinePlayer] {gameObject.name} IS local player - voicelines ENABLED");

        // Create audio source if not assigned
        if (voicelineAudioSource == null)
        {
            voicelineAudioSource = gameObject.AddComponent<AudioSource>();
        }

        // Configure audio source
        voicelineAudioSource.loop = false;
        voicelineAudioSource.playOnAwake = false;
        voicelineAudioSource.volume = voicelineVolume;
        voicelineAudioSource.spatialBlend = 0f; // 2D sound (not 3D spatial)

        // Validate voiceline clips
        if (voicelineClips.Length == 0 || voicelineClips[0] == null)
        {
            Debug.LogWarning($"[VoicelinePlayer] No voiceline clips assigned to {gameObject.name}");
            return;
        }

        // Start playing voicelines
        StartVoicelines();
    }

    public void StartVoicelines()
    {
        if (!isLocalPlayer)
        {
            Debug.Log($"[VoicelinePlayer] Cannot start voicelines - not local player");
            return;
        }

        if (isPlaying || gameEnded || isDead) return;

        isPlaying = true;
        voicelineCoroutine = StartCoroutine(PlayVoicelinesRandomly());
        Debug.Log($"[VoicelinePlayer] Started voicelines for {gameObject.name}");
    }

    public void StopVoicelines()
    {
        if (!isLocalPlayer) return;

        isPlaying = false;

        if (voicelineCoroutine != null)
        {
            StopCoroutine(voicelineCoroutine);
            voicelineCoroutine = null;
        }

        if (voicelineAudioSource != null && voicelineAudioSource.isPlaying)
        {
            voicelineAudioSource.Stop();
        }

        Debug.Log($"[VoicelinePlayer] Stopped voicelines for {gameObject.name}");
    }

    private IEnumerator PlayVoicelinesRandomly()
    {
        // Initial delay before first voiceline
        yield return new WaitForSeconds(initialDelay);

        while (isPlaying && !gameEnded && !isDead)
        {
            // Check if voicelines are enabled in SoundManager
            if (SoundManager.Instance != null && !SoundManager.Instance.voicelinesEnabled)
            {
                yield return new WaitForSeconds(1f); // Check again in 1 second
                continue;
            }

            // Pick a random voiceline
            AudioClip randomClip = GetRandomVoiceline();

            if (randomClip != null && voicelineAudioSource != null)
            {
                // Play the voiceline
                voicelineAudioSource.PlayOneShot(randomClip);
                Debug.Log($"[VoicelinePlayer] 🔊 Playing voiceline for LOCAL PLAYER: {gameObject.name}");

                // Wait for the clip to finish
                yield return new WaitForSeconds(randomClip.length);
            }

            // Wait random time before next voiceline
            float waitTime = Random.Range(minTimeBetweenVoicelines, maxTimeBetweenVoicelines);
            yield return new WaitForSeconds(waitTime);
        }
    }

    private AudioClip GetRandomVoiceline()
    {
        // Filter out null clips
        AudioClip[] validClips = System.Array.FindAll(voicelineClips, clip => clip != null);

        if (validClips.Length == 0)
        {
            Debug.LogWarning($"[VoicelinePlayer] No valid voiceline clips for {gameObject.name}");
            return null;
        }

        // Return random clip
        int randomIndex = Random.Range(0, validClips.Length);
        return validClips[randomIndex];
    }

    // Call this when the game ends
    public void OnGameEnded()
    {
        if (!isLocalPlayer) return;

        gameEnded = true;
        StopVoicelines();
    }

    // Call this when survivor dies
    public void OnSurvivorDeath()
    {
        if (!isLocalPlayer) return;

        isDead = true;
        StopVoicelines();
    }

    // Update volume from SoundManager
    public void UpdateVolume(float volume)
    {
        if (!isLocalPlayer) return;

        voicelineVolume = Mathf.Clamp01(volume);
        if (voicelineAudioSource != null)
        {
            voicelineAudioSource.volume = voicelineVolume;
        }
    }

    void OnDestroy()
    {
        StopVoicelines();
    }
}