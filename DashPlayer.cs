using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem.XR;
public class DashVideoPlayer : MonoBehaviour
{

    public string mpdURL = "http://192.168.1.201:3000/playable_split/videos/Manifest.mpd"; // URL to the MPD file
    public VideoPlayer videoPlayer1;
    public VideoPlayer videoPlayer2;
    public RenderTexture renderTexture1;
    public RenderTexture renderTexture2;
    public Material skyboxMaterial;
    public float switchTimeBeforeEnd = 0.4f; // Default time in seconds before the video ends to switch

    // UI Elements
    public InputField switchDelayInputField;
    public Button confirmButton;
    private MPDParser mpdParser;
    private SegmentFetcher segmentFetcher;
    private BufferManager bufferManager;
    private ABRAlgorithm abrAlgorithm;
    private MetricsLogger metricsLogger;
    private EyeQoEMetricsLogger eyeQoEMetricsLogger;
    public int segmentNumber = 1;

    private int startedTrackingeye = 0;

    void Start()
    {
        videoPlayer1.targetTexture = renderTexture1;
        videoPlayer2.targetTexture = renderTexture2;
        skyboxMaterial.mainTexture = renderTexture1;
        videoPlayer1.playOnAwake = false;
        videoPlayer2.playOnAwake = false;
        videoPlayer1.isLooping = false;
        videoPlayer2.isLooping = false;


        mpdParser = new MPDParser();
        segmentFetcher = gameObject.AddComponent<SegmentFetcher>();
        bufferManager = BufferManager.Instance;
        metricsLogger = gameObject.AddComponent<MetricsLogger>();
        eyeQoEMetricsLogger = gameObject.AddComponent<EyeQoEMetricsLogger>();
        abrAlgorithm = new ABRAlgorithm();

        bufferManager.InitializeBufferQueue();

         StartCoroutine(SetupVideoPlayer());

        // Add listener to the confirm button
        // confirmButton.onClick.AddListener(OnConfirmButtonClicked);
    }

    private void OnConfirmButtonClicked()
    {
        // Parse the input value and set the switch time before end
        if (float.TryParse(switchDelayInputField.text, out float switchDelay))
        {
            switchTimeBeforeEnd = switchDelay;
        }
        else
        {
            Debug.LogError("Invalid switch delay input. Using default value.");
        }

        // Hide UI elements after confirmation
        switchDelayInputField.gameObject.SetActive(false);
        confirmButton.gameObject.SetActive(false);

        // Start the video player setup
        StartCoroutine(SetupVideoPlayer());
    }

    IEnumerator SetupVideoPlayer()
    {
        yield return StartCoroutine(deleteAllExistingVideos());

        string[] files = null;
        if (GetPlatformCode() == 'L')
            files = Directory.GetFiles("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "*.webm");
        if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
            files = Directory.GetFiles(Application.persistentDataPath, "*.mp4");

        Debug.Log("-->>>>> Files Found : " + files.Length + " All deleted, continuing process");
        yield return StartCoroutine(mpdParser.FetchMPD(mpdURL));
        var representations = mpdParser.GetRepresentations();
        StartCoroutine(segmentFetcher.FetchSegments(representations, abrAlgorithm));

        // Prepare 2 video players in parallel
        StartCoroutine(PrepareSegment(videoPlayer1));
    }

    IEnumerator deleteAllExistingVideos()
    {
        string[] files = null;
        if (GetPlatformCode() == 'L')
            files = Directory.GetFiles("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "*.webm");
        if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
            files = Directory.GetFiles(Application.persistentDataPath, "*.mp4");

        Debug.Log("-->>>>> Files Found : " + files.Length + " Deleting...");
        foreach (string file in files)
        {
            File.Delete(file);
        }

        yield return null;
    }

    private bool checkIfFileExists(string filePath)
    {
        return File.Exists(filePath);
    }

    private IEnumerator PrepareSegment(VideoPlayer videoPlayer)
    {
        Debug.Log("Prepare segment function fired");
        int temp = 10;
        while (temp == 10)
        {
            string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
            if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
                filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");

            Debug.Log($"Processing Video: {filePath}");
            metricsLogger.LogBufferStatus(segmentNumber);

            if (checkIfFileExists(filePath))
            {
                Debug.Log("Prepared started for segment " + segmentNumber);
                videoPlayer.url = "file://" + filePath;
                videoPlayer.errorReceived += HandleVideoError;
                videoPlayer.prepareCompleted += PrepareCompleted;
                videoPlayer.Prepare();
                temp = 1;
                
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    void PrepareCompleted(VideoPlayer videoPlayer)
    {
        
        if (segmentNumber % 2 != 0)
        {
            videoPlayer = videoPlayer1;
        }
        else
        {
            videoPlayer = videoPlayer2;
        }
        int videoPlayerIndex = videoPlayer == videoPlayer1 ? 1 : 2;
        Debug.Log(segmentNumber + " Video Prepared by player " + videoPlayerIndex);
        if (videoPlayer == videoPlayer1)
        {
            skyboxMaterial.mainTexture = renderTexture1;
        }
        else
        {
            skyboxMaterial.mainTexture = renderTexture2;
        }
        videoPlayer.Play();
        if(startedTrackingeye == 0){
            startedTrackingeye = 1;
            eyeQoEMetricsLogger.StartLoggingEverySecond();
        }
        
        videoPlayer.prepareCompleted -= PrepareCompleted;

        videoPlayer.loopPointReached += OnVideoEnded;
        ++segmentNumber;
        StartCoroutine(PrepareNextSegment(videoPlayer));
        StartCoroutine(CheckIfPlayerIsPlayingAndNotify(videoPlayer));
    }

    private IEnumerator PrepareNextSegment(VideoPlayer videoPlayer)
    {
        Debug.Log("Prepare next segment function fired: " + segmentNumber);
        double timeRemaining = videoPlayer.length - videoPlayer.time;
        Debug.Log($"-->>>> Time remaining: {timeRemaining} seconds");
        yield return new WaitForSeconds((float)videoPlayer.length - switchTimeBeforeEnd);

        int temp = 100;
        while (temp == 100)
        {
            string nextFilePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
            if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
                nextFilePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");

            Debug.Log($"Processing Next Segment: {nextFilePath}");
            metricsLogger.LogBufferStatus(segmentNumber);

            if (checkIfFileExists(nextFilePath))
            {
                if (videoPlayer == videoPlayer1)
                {
                    videoPlayer2.url = nextFilePath;
                    videoPlayer2.prepareCompleted += PrepareCompleted;
                    videoPlayer2.Prepare();
                }
                else
                {
                    videoPlayer1.url = nextFilePath;
                    videoPlayer1.prepareCompleted += PrepareCompleted;
                    videoPlayer1.Prepare();
                }
                temp = 1;
                yield return null;
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void OnVideoEnded(VideoPlayer videoPlayer)
    {
        

        Debug.Log("Video ended");
        
        videoPlayer.loopPointReached -= OnVideoEnded;
    }

    void HandleVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("Error playing video: " + message);
    }

    public static char GetPlatformCode()
    {
        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        return 'W'; // Windows
        #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        return 'L'; // Linux
        #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        return 'M'; // macOS
        #elif UNITY_ANDROID
        return 'A'; // Android
        #elif UNITY_IOS
        return 'I'; // iOS
        #else
        return 'U'; // Unknown or unsupported platform
        #endif
    }

    IEnumerator CheckIfPlayerIsPlayingAndNotify(VideoPlayer videoPlayer)
    {
       while(!videoPlayer.isPlaying)
       {
           yield return new WaitForSeconds(0.1f);
       }
         eyeQoEMetricsLogger.IncreaseSegmentNumber();
    }
}

// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;
// using UnityEngine.UI;
// using UnityEngine.Video;

// public class DashVideoPlayer : MonoBehaviour
// {
//     public string mpdURL; // URL to the MPD file
//     public VideoPlayer videoPlayer1;
//     public VideoPlayer videoPlayer2;
//     public RenderTexture renderTexture1;
//     public RenderTexture renderTexture2;
//     public Material skyboxMaterial;
//     public float switchTimeBeforeEnd = 0.4f; // Default time in seconds before the video ends to switch

//     // UI Elements
//     public InputField switchDelayInputField;
//     public Button confirmButton;

//     private MPDParser mpdParser;
//     private SegmentFetcher segmentFetcher;
//     private BufferManager bufferManager;
//     private ABRAlgorithm abrAlgorithm;
//     private MetricsLogger metricsLogger;
//     private EyeQoEMetricsLogger eyeQoEMetricsLogger;
//     public int segmentNumber = 1;

//     public bool IsVideoFinished { get; private set; }

//     public void StartPlayBack(string mpdUrl, System.Action onVideoFinishedCallback, RawImage bufferScreen)
//     {
//         Debug.Log("->>>>> DashVideoPlayer Start function fired with url: " + mpdUrl);
//         videoPlayer1.targetTexture = renderTexture1;
//         videoPlayer2.targetTexture = renderTexture2;
//         skyboxMaterial.mainTexture = renderTexture1;

//         mpdParser = new MPDParser();
//         segmentFetcher = gameObject.AddComponent<SegmentFetcher>();
//         bufferManager = BufferManager.Instance;
//         metricsLogger = gameObject.AddComponent<MetricsLogger>();
//         eyeQoEMetricsLogger = gameObject.AddComponent<EyeQoEMetricsLogger>();
//         abrAlgorithm = new ABRAlgorithm();

//         bufferManager.InitializeBufferQueue();

//         SetupVideoPlayer(mpdUrl, onVideoFinishedCallback, bufferScreen);
//     }

//     public void SetupVideoPlayer(string mpdURL, System.Action onVideoFinishedCallback, RawImage bufferScreen)
//     {
//         this.mpdURL = mpdURL;
//         IsVideoFinished = false;
//         StartCoroutine(SetupVideoPlayerCoroutine(onVideoFinishedCallback));
//     }

//     private IEnumerator SetupVideoPlayerCoroutine(System.Action onVideoFinishedCallback)
//     {
//         yield return StartCoroutine(deleteAllExistingVideos());
//         string[] files = null;
//         if (GetPlatformCode() == 'L')
//             files = Directory.GetFiles("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "*.webm");
//         if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
//             files = Directory.GetFiles(Application.persistentDataPath, "*.mp4");

//         Debug.Log("-->>>>> Files Found : " + files.Length + " All deleted, continuing process");

//         yield return StartCoroutine(mpdParser.FetchMPD(mpdURL));
//         var representations = mpdParser.GetRepresentations();
//         StartCoroutine(segmentFetcher.FetchSegments(representations, abrAlgorithm));

//         // Prepare 2 video players in parallel
//         StartCoroutine(PrepareSegment(videoPlayer1, onVideoFinishedCallback));
//     }

//     IEnumerator deleteAllExistingVideos()
//     {
//         string[] files = null;
//         if (GetPlatformCode() == 'L')
//             files = Directory.GetFiles("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "*.webm");
//         if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
//             files = Directory.GetFiles(Application.persistentDataPath, "*.mp4");

//         Debug.Log("-->>>>> Files Found : " + files.Length + " Deleting...");
//         foreach (string file in files)
//         {
//             File.Delete(file);
//         }

//         yield return null;
//     }

//     private bool checkIfFileExists(string filePath)
//     {
//         return File.Exists(filePath);
//     }

//     private IEnumerator PrepareSegment(VideoPlayer videoPlayer, System.Action onVideoFinishedCallback)
//     {
//         Debug.Log("Prepare segment function fired");
//         int temp = 10;
//         while (temp == 10)
//         {
//             string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
//             if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
//                 filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");

//             Debug.Log($"Processing Video: {filePath}");
//             metricsLogger.LogBufferStatus(segmentNumber);

//             if (checkIfFileExists(filePath))
//             {
//                 Debug.Log("Prepared started for segment " + segmentNumber);
//                 videoPlayer.url = "file://" + filePath;
//                 videoPlayer.errorReceived += HandleVideoError;
//                 videoPlayer.prepareCompleted += PrepareCompleted;
//                 videoPlayer.Prepare();
//                 temp = 1;
//                 yield return null;
//             }
//             else
//             {
//                 yield return new WaitForSeconds(0.1f);
//             }
//         }
//     }

//     void PrepareCompleted(VideoPlayer videoPlayer)
//     {
//         if (segmentNumber % 2 != 0)
//         {
//             videoPlayer = videoPlayer1;
//         }
//         else
//         {
//             videoPlayer = videoPlayer2;
//         }
//         int videoPlayerIndex = videoPlayer == videoPlayer1 ? 1 : 2;
//         Debug.Log(segmentNumber + " Video Prepared by player " + videoPlayerIndex);
//         if (videoPlayer == videoPlayer1)
//         {
//             skyboxMaterial.mainTexture = renderTexture1;
//         }
//         else
//         {
//             skyboxMaterial.mainTexture = renderTexture2;
//         }
//         videoPlayer.Play();
//         videoPlayer.prepareCompleted -= PrepareCompleted;

//         videoPlayer.loopPointReached += OnVideoEnded;
//         ++segmentNumber;
//         StartCoroutine(PrepareNextSegment(videoPlayer));
//     }

//     private IEnumerator PrepareNextSegment(VideoPlayer videoPlayer)
//     {
//         Debug.Log("Prepare next segment function fired: " + segmentNumber);
//         double timeRemaining = videoPlayer.length - videoPlayer.time;
//         Debug.Log($"-->>>> Time remaining: {timeRemaining} seconds");
//         yield return new WaitForSeconds((float)videoPlayer.length - switchTimeBeforeEnd);

//         int temp = 100;
//         while (temp == 100)
//         {
//             string nextFilePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
//             if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
//                 nextFilePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");

//             Debug.Log($"Processing Next Segment: {nextFilePath}");
//             metricsLogger.LogBufferStatus(segmentNumber);

//             if (checkIfFileExists(nextFilePath))
//             {
//                 if (videoPlayer == videoPlayer1)
//                 {
//                     videoPlayer2.url = nextFilePath;
//                     videoPlayer2.prepareCompleted += PrepareCompleted;
//                     videoPlayer2.Prepare();
//                 }
//                 else
//                 {
//                     videoPlayer1.url = nextFilePath;
//                     videoPlayer1.prepareCompleted += PrepareCompleted;
//                     videoPlayer1.Prepare();
//                 }
//                 temp = 1;
//                 yield return null;
//             }
//             else
//             {
//                 yield return new WaitForSeconds(0.1f);
//             }
//         }
//     }

//     private void OnVideoEnded(VideoPlayer videoPlayer)
//     {
//         Debug.Log("Video ended");
//         videoPlayer.loopPointReached -= OnVideoEnded;
//         if(segmentNumber >= 5){
//             IsVideoFinished = true;
//         }
//     }

//     void HandleVideoError(VideoPlayer vp, string message)
//     {
//         Debug.LogError("Error playing video: " + message);
//         IsVideoFinished = true;
//     }

//     public static char GetPlatformCode()
//     {
//         #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
//         return 'W'; // Windows
//         #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
//         return 'L'; // Linux
//         #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
//         return 'M'; // macOS
//         #elif UNITY_ANDROID
//         return 'A'; // Android
//         #elif UNITY_IOS
//         return 'I'; // iOS
//         #else
//         return 'U'; // Unknown or unsupported platform
//         #endif
//     }
// }
