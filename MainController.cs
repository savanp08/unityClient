using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class DashVideoMaster : MonoBehaviour
{
    public string baseURL = "http://192.168.1.201:3000/exp/videos/video";
    public Material skyboxMaterial; // Material for skybox passed to DashVideoPlayer
    private int currentIndex = 0;
    public int currentVideoID = 0;
    public int currentQualityIndex = 0;
    public List<(int videoID, int qualityIndex)> videoQualityPairs;
    
    private List<float> switchTimes = new List<float>();

    void Start()
    {
        MoveFilesToNewSessionFolder();

        switchTimes = new List<float>
        {
            0.01f, // phasre 4
            0.25f, // elephant
            0.32f, // snow skating
            0.2f,  // driving 
            0.17f, // park
            0.24f, // packman
            0.21f, // beach
            0.23f, // hog rider
            0.193f,// rihnos
            0.31f, // sun rise
            0.3f,  // painting hall   // 0.3f 80%
            0.25f, // watefall open   // 0.25f 70%
            0.2f,  // driving 2 single car   // 0.2f 80%
            0.2f,  // trees
            0.2f,  // waterfall closed
            0.2f,  // snow skating 2
            0.0f   // None
        };

        // Generate video-quality pairs
        videoQualityPairs = new List<(int videoID, int qualityIndex)>();
        for (int i = 0; i < 5; i++) // Assuming 15 videos
        {
            int RandomQuality = Random.Range(0, 3);
            videoQualityPairs.Add((i, RandomQuality));
            Debug.Log("debug 19 --->>>> main controller : Video " + i + " quality " + RandomQuality);

        }

        // Shuffle the list to ensure randomness
        videoQualityPairs.Shuffle();

        if (videoQualityPairs.Count > 0)
        {
            Debug.Log("Starting video playback.");
            PlayNextVideo();
        }
    }

    public void OnVideoEnded()
    {
        Debug.Log("debug 19 --->>>> main controller : Video ended.");
        currentIndex++;

        if (currentIndex < videoQualityPairs.Count)
        {
            PlayNextVideo();
        }
        else
        {
            Debug.Log("debug 19 --->>>> main controller : All videos have been played.");
        }
    }

    public void OnCleanupComplete()
    {
        // Cleanup actions after DashVideoPlayer has finished and cleaned up
        Debug.Log("DashVideoPlayer cleanup complete.");
    }

    private void PlayNextVideo()
    {
        if (currentIndex < videoQualityPairs.Count)
        {
            // createa  new csv file with the video id
            currentVideoID = videoQualityPairs[currentIndex].videoID;
            string csvPath = Path.Combine(Application.persistentDataPath, $"Session{currentVideoID}.csv");
            File.WriteAllText(csvPath, "Timestamp, Arrival Time, FinishTime");
            (currentVideoID, currentQualityIndex) = videoQualityPairs[currentIndex];
           
           string arrivalTimeTextFile =  Path.Combine(Application.persistentDataPath, $"Session{currentVideoID}ArrivalTime.txt");
              File.WriteAllText(arrivalTimeTextFile, "Arrival Time");
           string finishTimeTextFile =  Path.Combine(Application.persistentDataPath, $"Session{currentVideoID}FinishTime.txt");
              File.WriteAllText(finishTimeTextFile, "FinishTime");
           
            string mpdURL = $"{baseURL}{currentVideoID}/Manifest.mpd";
            Debug.Log($"Playing video: {mpdURL} at quality index: {currentQualityIndex}");

            // Create a new DashVideoPlayer instance
            GameObject dashVideoPlayerObject = new GameObject("DashVideoPlayer");
            DashVideoPlayer dashVideoPlayer = dashVideoPlayerObject.AddComponent<DashVideoPlayer>();

            // Set the quality index before playing the video
            dashVideoPlayer.qualityIndex = currentQualityIndex;

            // Initialize DashVideoPlayer with the required parameters
            dashVideoPlayer.Initialize(skyboxMaterial, this);

            // Play the video
            if (currentIndex < switchTimes.Count)
            {
                dashVideoPlayer.PlayVideo(mpdURL, switchTimes[0], currentVideoID);
            }
            else
            {
                dashVideoPlayer.PlayVideo(mpdURL, 0.1f, currentVideoID);
            }
        }
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

public static class ListExtensions
{
    private static System.Random rng = new System.Random();

    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
