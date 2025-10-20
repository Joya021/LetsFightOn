// COMPLETE REPLACEMENT for StunnableScript.cs
// This version supports BOTH HunterController (multiplayer) and HunterChaseAndHack (offline)

using UnityEngine;
using Photon.Pun;
using System.Collections;

public class StunnableScript : MonoBehaviourPunCallbacks, IPunObservable
{
    public float stunDuration = 3f;
    public bool IsStunned { get; private set; }
    private float stunTimeRemaining = 0f;
    private bool isUsingLocalStunTimer = false;

    private double stunEndTime = 0f;
    private PlayerMovement playerMovement;
    private HunterController hunterController;
    private HunterChaseAndHack hunterChaseAndHack; // NEW: Support for offline AI hunter
    private SpriteRenderer sr;
    private PhotonView photonView;
    private bool isMultiplayer = false;
    private Color originalColor;

    // Network sync
    private bool networkStunned = false;
    private double networkStunEndTime = 0f;

    // Effect tracking
    private bool isAffectedByGrog = false;
    private bool isAffectedByAuraFarm = false;
    private bool isHealed = false;
    private double grogEndTime = 0f;
    private double auraFarmEndTime = 0f;
    private Coroutine grogEffectCoroutine;
    private Coroutine auraFarmEffectCoroutine;
    private Coroutine healEffectCoroutine;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        hunterController = GetComponent<HunterController>();
        hunterChaseAndHack = GetComponent<HunterChaseAndHack>(); // NEW: Get offline hunter component
        sr = GetComponent<SpriteRenderer>();
        photonView = GetComponent<PhotonView>();

        isMultiplayer = PhotonNetwork.IsConnected && photonView != null;

        if (sr != null)
        {
            originalColor = sr.color;
            sr.color = Color.white;
        }

        // Debug what components we found
        Debug.Log($"[STUNNABLE START] {gameObject.name} components:");
        Debug.Log($"  - PlayerMovement: {playerMovement != null}");
        Debug.Log($"  - HunterController: {hunterController != null}");
        Debug.Log($"  - HunterChaseAndHack: {hunterChaseAndHack != null}");
        Debug.Log($"  - IsMultiplayer: {isMultiplayer}");
    }

    void Update()
    {
        bool isLocalPlayer = !photonView || photonView.IsMine;

        if (isMultiplayer && !photonView.IsMine)
        {
            IsStunned = networkStunned;
            if (IsStunned)
            {
                double timeRemaining = networkStunEndTime - PhotonNetwork.Time;
                if (timeRemaining <= 0f)
                {
                    IsStunned = false;
                    networkStunned = false;
                }
            }
            return;
        }

        // Handle stun with LOCAL timer (for offline mode)
        if (IsStunned)
        {
            double timeRemaining = 0f;

            if (isUsingLocalStunTimer)
            {
                stunTimeRemaining -= Time.deltaTime;
                timeRemaining = stunTimeRemaining;

                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[STUNNABLE UPDATE] {gameObject.name} stunned (LOCAL TIMER) - time remaining: {timeRemaining:F1}s");
                }

                if (timeRemaining <= 0f)
                {
                    Debug.Log($"[STUNNABLE UPDATE] {gameObject.name} stun expired - calling Unstun()");
                    Unstun();
                    return;
                }
            }
            else
            {
                timeRemaining = stunEndTime - PhotonNetwork.Time;

                if (Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[STUNNABLE UPDATE] {gameObject.name} stunned (PHOTON TIMER) - time remaining: {timeRemaining:F1}s");
                }

                if (timeRemaining <= 0f)
                {
                    Debug.Log($"[STUNNABLE UPDATE] {gameObject.name} stun expired - calling Unstun()");
                    Unstun();
                    return;
                }
            }

            // Update stun duration panel (only for local player)
            if (isLocalPlayer && playerMovement != null && playerMovement.stunEffectPanel != null)
            {
                playerMovement.stunEffectPanel.SetActive(true);
                if (playerMovement.stunEffectText != null)
                    playerMovement.stunEffectText.text = $"Stunned: {timeRemaining:F1}s";
            }
            else if (isLocalPlayer && hunterController != null && hunterController.stunEffectPanel != null)
            {
                hunterController.stunEffectPanel.SetActive(true);
                if (hunterController.stunEffectText != null)
                    hunterController.stunEffectText.text = $"Stunned: {timeRemaining:F1}s";
            }
        }
        else
        {
            isUsingLocalStunTimer = false;

            // Hide stun panel when not stunned (only for local player)
            if (isLocalPlayer && playerMovement != null && playerMovement.stunEffectPanel != null)
                playerMovement.stunEffectPanel.SetActive(false);
            else if (isLocalPlayer && hunterController != null && hunterController.stunEffectPanel != null)
                hunterController.stunEffectPanel.SetActive(false);
        }

        // Update Grog effect panel (only for survivors and local player)
        if (isAffectedByGrog && isLocalPlayer && playerMovement != null)
        {
            double timeRemaining = grogEndTime - PhotonNetwork.Time;
            if (playerMovement.grogEffectPanel != null)
            {
                playerMovement.grogEffectPanel.SetActive(true);
                if (playerMovement.grogEffectText != null)
                    playerMovement.grogEffectText.text = $"Grogged: {timeRemaining:F1}s";
            }
        }
        else if (isLocalPlayer && playerMovement != null && playerMovement.grogEffectPanel != null)
        {
            playerMovement.grogEffectPanel.SetActive(false);
        }

        // Update Aura Farm effect panel (only for survivors and local player)
        if (isAffectedByAuraFarm && isLocalPlayer && playerMovement != null)
        {
            double timeRemaining = auraFarmEndTime - PhotonNetwork.Time;
            if (playerMovement.auraFarmEffectPanel != null)
            {
                playerMovement.auraFarmEffectPanel.SetActive(true);
                if (playerMovement.auraFarmEffectText != null)
                    playerMovement.auraFarmEffectText.text = $"Aura Farm: {timeRemaining:F1}s";
            }
        }
        else if (isLocalPlayer && playerMovement != null && playerMovement.auraFarmEffectPanel != null)
        {
            playerMovement.auraFarmEffectPanel.SetActive(false);
        }
    }

    public void Stun()
    {
        Stun(stunDuration);
    }

    public void Stun(float duration)
    {
        Debug.Log($"[STUN] ⚡ {gameObject.name} is being stunned for {duration} seconds!");
        Debug.Log($"[STUN] Was already stunned: {IsStunned}");

        IsStunned = true;

        // Use local timer for offline mode
        if (!isMultiplayer)
        {
            isUsingLocalStunTimer = true;
            stunTimeRemaining = duration;
            Debug.Log($"[STUN] Using LOCAL timer (offline mode): {stunTimeRemaining}s");
        }
        else
        {
            // Use PhotonNetwork.Time for multiplayer
            stunEndTime = PhotonNetwork.Time + duration;
            isUsingLocalStunTimer = false;
            Debug.Log($"[STUN] Using PHOTON timer (multiplayer mode)");
        }

        // Lock movement for survivors
        if (playerMovement != null)
        {
            Debug.Log("[STUN] Locking PlayerMovement");
            playerMovement.LockMovement(duration);
        }

        // Lock movement for multiplayer hunters
        if (hunterController != null)
        {
            Debug.Log("[STUN] Locking HunterController");
            hunterController.LockMovement(duration);
        }

        // Lock movement for offline AI hunters (no explicit call needed - IsStunned check handles it)
        if (hunterChaseAndHack != null)
        {
            Debug.Log("[STUN] Locking HunterChaseAndHack (offline AI)");
        }

        UpdateColor();

        // Sync stun across network (only if multiplayer)
        if (isMultiplayer && photonView != null && photonView.IsMine)
        {
            photonView.RPC("RPC_Stun", RpcTarget.Others, duration);
        }

        Debug.Log($"[STUN] ✅✅✅ Stun applied successfully to {gameObject.name}!");
    }

    [PunRPC]
    void RPC_Stun(float duration)
    {
        if (IsStunned) return;

        IsStunned = true;
        stunEndTime = PhotonNetwork.Time + duration;

        Debug.Log($"{gameObject.name} received network stun for {duration} seconds!");

        if (playerMovement != null)
            playerMovement.LockMovement(duration);

        if (hunterController != null)
            hunterController.LockMovement(duration);

        // HunterChaseAndHack doesn't need explicit locking - it checks IsStunned

        UpdateColor();
    }

    public void Unstun()
    {
        IsStunned = false;

        Debug.Log($"[UNSTUN] ✅ {gameObject.name} is no longer stunned.");

        if (playerMovement != null)
        {
            Debug.Log("[UNSTUN] Unlocking PlayerMovement");
            playerMovement.UnlockMovement();
        }

        if (hunterController != null)
        {
            Debug.Log("[UNSTUN] Unlocking HunterController");
            hunterController.UnlockMovement();
        }

        // HunterChaseAndHack will automatically resume movement when IsStunned = false

        // Update color without stun effect
        UpdateColor();

        // Sync unstun across network
        if (isMultiplayer && photonView != null && photonView.IsMine)
        {
            photonView.RPC("RPC_Unstun", RpcTarget.Others);
        }
    }

    [PunRPC]
    public void RPC_Unstun()
    {
        IsStunned = false;

        Debug.Log($"{gameObject.name} received network unstun.");

        if (playerMovement != null)
            playerMovement.UnlockMovement();

        if (hunterController != null)
            hunterController.UnlockMovement();

        UpdateColor();
    }

    // Grog effect methods
    public void ApplyGrogEffect(float duration)
    {
        if (grogEffectCoroutine != null)
            StopCoroutine(grogEffectCoroutine);

        grogEffectCoroutine = StartCoroutine(GrogEffectCoroutine(duration));
    }

    private IEnumerator GrogEffectCoroutine(float duration)
    {
        isAffectedByGrog = true;
        grogEndTime = PhotonNetwork.Time + duration;
        UpdateColor();

        yield return new WaitForSeconds(duration);

        isAffectedByGrog = false;
        UpdateColor();
        grogEffectCoroutine = null;
    }

    // Aura Farm effect methods
    public void ApplyAuraFarmEffect(float duration)
    {
        if (auraFarmEffectCoroutine != null)
            StopCoroutine(auraFarmEffectCoroutine);

        auraFarmEffectCoroutine = StartCoroutine(AuraFarmEffectCoroutine(duration));
    }

    private IEnumerator AuraFarmEffectCoroutine(float duration)
    {
        isAffectedByAuraFarm = true;
        auraFarmEndTime = PhotonNetwork.Time + duration;
        UpdateColor();

        yield return new WaitForSeconds(duration);

        isAffectedByAuraFarm = false;
        UpdateColor();
        auraFarmEffectCoroutine = null;
    }

    // Heal effect methods
    public void ApplyHealEffect(float duration = 3f)
    {
        if (healEffectCoroutine != null)
            StopCoroutine(healEffectCoroutine);

        healEffectCoroutine = StartCoroutine(HealEffectCoroutine(duration));
    }

    private IEnumerator HealEffectCoroutine(float duration)
    {
        isHealed = true;
        UpdateColor();

        yield return new WaitForSeconds(duration);

        isHealed = false;
        UpdateColor();
        healEffectCoroutine = null;
    }

    // Color priority: Stun (blue) > Heal (green) > Grog (violet) > Aura Farm (yellow) > Original
    private void UpdateColor()
    {
        if (sr == null) return;

        if (IsStunned)
        {
            sr.color = Color.blue;
            Debug.Log($"[COLOR] {gameObject.name} color set to BLUE (stunned)");
        }
        else if (isHealed)
        {
            sr.color = Color.green;
        }
        else if (isAffectedByGrog)
        {
            sr.color = new Color(0.5f, 0f, 1f); // Violet
        }
        else if (isAffectedByAuraFarm)
        {
            sr.color = Color.yellow;
        }
        else
        {
            sr.color = originalColor;
            Debug.Log($"[COLOR] {gameObject.name} color set to ORIGINAL");
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(IsStunned);
            stream.SendNext(stunEndTime);
        }
        else
        {
            networkStunned = (bool)stream.ReceiveNext();
            networkStunEndTime = (double)stream.ReceiveNext();
        }
    }
}