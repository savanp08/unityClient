using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DashVideoMaster : MonoBehaviour
{
    public string[] mpdURLs = new string[] {
        // Add your MPD URLs here
    };
    public Material skyboxMaterial; // Material for skybox passed to DashVideoPlayer
    private int currentIndex = 0;
    private int currentQualityIndex = 0;
     private List<float> switchTimes = new List<float>();

    void Start()
    {
        MoveFilesToNewSessionFolder();
        switchTimes = new List<float>
        {
            
            
            0.25f, // elephant
            0.32f,   // snow skating
            0.2f,   // driving 
            
             0.17f,   // park
            0.24f,  //packman
            0.21f,    // beach
            0.23f,   // hog rider
            
            0.193f,   //rihnos
            0.31f,   // sun rise
            0.3f,   // painting hall   // 0.3f 80%
            0.25f,     // watefall open     // 0.25f 70%
            0.2f,   // driving 2 single car   // 0.2f 80%
            0.2f,  // trees
            0.2f,   //waterfall closed
            0.2f,   // snow skating 2

            
            0.0f    // None
        };
        if (mpdURLs.Length > 0)
        {
            Debug.Log(" ------>>>> from master : Starting video playback.");
            PlayNextVideo();
        }
    }

    public void OnVideoEnded()
    {
        Debug.Log(" ------>>>> debug 19 from master : Video ended.");
        currentQualityIndex+=4;
        if (currentQualityIndex > 2)
        {
            currentQualityIndex = 0;
            currentIndex++;
        }

        if (currentIndex < mpdURLs.Length)
        {
            PlayNextVideo();
        }
        else
        {
            Debug.Log("------->>>>> from master : All videos have been played.");
        }
    }

    public void OnCleanupComplete()
    {
        // Cleanup actions after DashVideoPlayer has finished and cleaned up
        Debug.Log("------->>>>> from master : DashVideoPlayer cleanup complete.");
    }

    private void PlayNextVideo()
    {
        if (currentIndex < mpdURLs.Length)
        {
            Debug.Log($" ------>>>> from master : Playing video: {mpdURLs[currentIndex]} at quality index: {currentQualityIndex}");

            // Create a new DashVideoPlayer instance
            GameObject dashVideoPlayerObject = new GameObject("DashVideoPlayer");
            DashVideoPlayer dashVideoPlayer = dashVideoPlayerObject.AddComponent<DashVideoPlayer>();

            // Initialize DashVideoPlayer with the required parameters
            dashVideoPlayer.Initialize(skyboxMaterial, this);

            // Set the quality index before playing the video
            dashVideoPlayer.qualityIndex = currentQualityIndex;

            // Play the video
            if(currentIndex < switchTimes.Count)
            {
                dashVideoPlayer.PlayVideo(mpdURLs[currentIndex], switchTimes[currentIndex]);
            }
            else
            {
            dashVideoPlayer.PlayVideo(mpdURLs[currentIndex]);
            }
        }
    }

    public string GetCurrentMPDURL()
    {
        if (currentIndex < mpdURLs.Length)
        {
            return mpdURLs[currentIndex];
        }
        return null;
    }
    public static void MoveFilesToNewSessionFolder()
    {
        string persistentPath = Application.persistentDataPath;
        string[] existingSessions = Directory.GetDirectories(persistentPath, "Session*");

        // Find the next available session index
        int sessionIndex = 1;
        while (Directory.Exists(Path.Combine(persistentPath, $"Session{sessionIndex}")))
        {
            sessionIndex++;
        }

        // Create the new session folder
        string newSessionFolder = Path.Combine(persistentPath, $"Session{sessionIndex}");
        Directory.CreateDirectory(newSessionFolder);

        // Get all files in the persistent path
        string[] files = Directory.GetFiles(persistentPath);

        // Move each file to the new session folder
        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string destFile = Path.Combine(newSessionFolder, fileName);
            File.Move(file, destFile);
        }

        Debug.Log($"Moved {files.Length} files to {newSessionFolder}");
    }
}
