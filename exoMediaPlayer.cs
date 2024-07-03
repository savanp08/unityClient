using System.Runtime.InteropServices;
using UnityEngine;

public class ExoPlayerController : MonoBehaviour
{
    private AndroidJavaObject exoPlayerManager;
    private RenderTexture renderTexture;
    public Material skyboxMaterial; // Reference to the existing skybox material

    void Start()
    {
        // Initialize Render Texture
        renderTexture = new RenderTexture(1920, 1080, 0);
        if (skyboxMaterial != null)
        {
            skyboxMaterial.mainTexture = renderTexture;
        }

        // Get the current Android activity and context
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            var context = activity.Call<AndroidJavaObject>("getApplicationContext");

            // Initialize ExoPlayerManager
            exoPlayerManager = new AndroidJavaObject("com.example.exopluginlib.ExoPlayerManager");
            int textureId = renderTexture.GetNativeTexturePtr().ToInt32();
            exoPlayerManager.CallStatic("initialize", context, textureId);
            exoPlayerManager.CallStatic("initializePlayerFromUnity", textureId);
        }
    }

    void Update()
    {
        // Add controls to play, pause, stop from Unity if needed
        if (Input.GetKeyDown(KeyCode.P))
        {
            exoPlayerManager.CallStatic("playFromUnity");
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            exoPlayerManager.CallStatic("pauseFromUnity");
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            exoPlayerManager.CallStatic("stopFromUnity");
        }
    }
}

