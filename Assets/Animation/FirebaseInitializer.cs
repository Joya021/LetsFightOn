using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;

public class FirebaseInitializer : MonoBehaviour
{
    private FirebaseApp firebaseApp;

    // Start is called before the first frame update
    void Start()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            FirebaseApp app = FirebaseApp.DefaultInstance;
            if (app == null)
            {
                Debug.LogError("Firebase initialization failed!");
            }
            else
            {
                Debug.Log("Firebase initialized successfully.");
            }
        });
    }
}
