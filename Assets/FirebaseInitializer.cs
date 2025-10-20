using Firebase;
using Firebase.Auth;
using Firebase.Database;
using Firebase.Extensions;
using UnityEngine;
using System;

public class FirebaseInitializer : MonoBehaviour
{
    public static FirebaseInitializer Instance { get; private set; }

    private FirebaseApp firebaseApp;
    private bool isInitialized = false;

    // Event to notify when Firebase is ready
    public static event Action OnFirebaseReady;

    void Awake()
    {
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
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        if (isInitialized)
        {
            Debug.Log("Firebase already initialized.");
            return;
        }

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            var dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                try
                {
                    // Get or create the default Firebase app
                    firebaseApp = FirebaseApp.DefaultInstance;

                    // Set database URL
                    var options = firebaseApp.Options;
                    if (options.DatabaseUrl == null)
                    {
                        options.DatabaseUrl = new System.Uri("https://fightontestfirebase-default-rtdb.asia-southeast1.firebasedatabase.app/");
                    }

                    // Initialize Firebase services
                    FirebaseDatabase.DefaultInstance.SetPersistenceEnabled(false);

                    isInitialized = true;
                    Debug.Log("✅ Firebase initialized successfully.");

                    // Notify all listeners that Firebase is ready
                    OnFirebaseReady?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Firebase initialization error: {ex.Message}");
                    isInitialized = false;
                }
            }
            else
            {
                Debug.LogError($"❌ Could not resolve all Firebase dependencies: {dependencyStatus}");
                isInitialized = false;
            }
        });
    }

    public bool IsFirebaseReady()
    {
        return isInitialized;
    }

    public void EnsureInitialized(Action onReady)
    {
        if (isInitialized)
        {
            onReady?.Invoke();
        }
        else
        {
            // Subscribe to the ready event
            Action handler = null;
            handler = () =>
            {
                OnFirebaseReady -= handler;
                onReady?.Invoke();
            };
            OnFirebaseReady += handler;
        }
    }
}