
using UnityEngine;
using Photon.Pun;

public class StunnableScript : MonoBehaviourPunCallbacks, IPunObservable
{
    public float stunDuration = 3f;
    public bool IsStunned { get; private set; }

    private double stunEndTime = 0f;
    private PlayerMovement playerMovement;
    private SpriteRenderer sr;
    private PhotonView photonView;
    private bool isMultiplayer = false;

    // Network sync
    private bool networkStunned = false;
    private double networkStunEndTime = 0f;

    void Start()
    {
        playerMovement = GetComponent<PlayerMovement>();
        sr = GetComponent<SpriteRenderer>();
        photonView = GetComponent<PhotonView>();

        isMultiplayer = PhotonNetwork.IsConnected && photonView != null;

        if (sr != null)
            sr.color = Color.white;
    }

    void Update()
    {
        // Handle network stun sync
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

        // Local stun logic
        if (IsStunned)
        {
            double timeRemaining = stunEndTime - PhotonNetwork.Time;
            if (timeRemaining <= 0f)
                Unstun();
        }
    }

    public void Stun()
    {
        Stun(stunDuration);
    }

    public void Stun(float duration)
    {
        if (IsStunned) return;

        IsStunned = true;
        stunEndTime = PhotonNetwork.Time + duration;

        Debug.Log($"{gameObject.name} is stunned for {duration} seconds!");

        if (playerMovement != null)
            playerMovement.LockMovement(duration);

        if (sr != null)
            sr.color = Color.blue;

        // Sync stun across network
        if (isMultiplayer && photonView != null && photonView.IsMine)
        {
            photonView.RPC("RPC_Stun", RpcTarget.Others, duration);
        }
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

        if (sr != null)
            sr.color = Color.blue;
    }

    void Unstun()
    {
        IsStunned = false;

        Debug.Log($"{gameObject.name} is no longer stunned.");

        if (playerMovement != null)
            playerMovement.UnlockMovement();

        if (sr != null)
            sr.color = Color.white;

        // Sync unstun across network
        if (isMultiplayer && photonView != null && photonView.IsMine)
        {
            photonView.RPC("RPC_Unstun", RpcTarget.Others);
        }
    }

    [PunRPC]
    void RPC_Unstun()
    {
        IsStunned = false;

        Debug.Log($"{gameObject.name} received network unstun.");

        if (playerMovement != null)
            playerMovement.UnlockMovement();

        if (sr != null)
            sr.color = Color.white;
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