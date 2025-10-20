using UnityEngine;

/// <summary>
/// Forces ALL devices to render at 1920x1080 landscape resolution
/// Attach this to an empty GameObject in your first scene
/// Name the GameObject "ResolutionManager" or similar
/// </summary>
public class Forcy : MonoBehaviour
{
    void Awake()
    {
        // Force landscape orientation
        Screen.orientation = ScreenOrientation.LandscapeLeft;
        Screen.autorotateToLandscapeLeft = true;
        Screen.autorotateToLandscapeRight = true;
        Screen.autorotateToPortrait = false;
        Screen.autorotateToPortraitUpsideDown = false;

        // Force 1920x1080 resolution
        Screen.SetResolution(1920, 1080, true);

        Debug.Log($"Forced resolution to 1920x1080. Current screen: {Screen.width}x{Screen.height}");
    }
}