using UnityEngine;

public class ExoPlayerPlugin : MonoBehaviour
{
    private AndroidJavaObject exoPlayerObject;

    void Start()
    {
        // Initialize the ExoPlayer plugin
        Debug.Log(" ---->>>>>> Initializing ExoPlayer...");
        InitializeExoPlayer();
    }

    private void InitializeExoPlayer()
    {
        try
        {
            // Get the UnityPlayer class and current activity
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Load the ExoPlayer class from the plugin
            AndroidJavaClass exoPlayerClass = new AndroidJavaClass("com.example.exopluginlib.ExoPlayerManager");
            Debug.Log(" ---->>>>>> ExoPlayer class loaded successfully.");
            
            // Create an instance of ExoPlayer
            exoPlayerObject = exoPlayerClass.CallStatic<AndroidJavaObject>("getInstance", currentActivity);

            if(exoPlayerObject == null)
            {
                Debug.Log(" ---->>>>>> Failed to initialize ExoPlayer: exoPlayerObject is null");
                return;
            }
            else{
                Debug.Log(" ---->>>>>> ExoPlayer initialized successfully.");
            }
            // list available methods on exoPlayerObject
            // string[] methods = exoPlayerObject.Call<string[]>("listMethods");
            


            // You can now call methods on exoPlayerObject as defined in your Java plugin
        }
        catch (System.Exception e)
        {
            Debug.LogError(" ---->>>>>> Failed to initialize ExoPlayer: " + e.Message);
        }
    }

    public void PlayVideo(string url)
    {
        if (exoPlayerObject != null)
        {
            exoPlayerObject.Call("playVideo", url);
        }
    }

    public void PauseVideo()
    {
        if (exoPlayerObject != null)
        {
            exoPlayerObject.Call("pauseVideo");
        }
    }

    public void StopVideo()
    {
        if (exoPlayerObject != null)
        {
            exoPlayerObject.Call("stopVideo");
        }
    }

    // Additional methods for controlling the video playback can be added here
}
