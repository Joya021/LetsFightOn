using UnityEngine;

public class InternetChecker : MonoBehaviour
{
    public GameObject noInternetPanel; // Assign this in inspector

    void Start()
    {
        CheckInternet();
    }

    public void CheckInternet()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Debug.LogWarning("No internet connection.");
            ShowNoInternetPanel(true);
        }
        else
        {
            Debug.Log("Internet connection detected.");
            ShowNoInternetPanel(false);
        }
    }

    void ShowNoInternetPanel(bool show)
    {
        if (noInternetPanel != null)
            noInternetPanel.SetActive(show);
    }
}
