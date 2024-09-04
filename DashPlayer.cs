using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System;
using Random = System.Random;

public class DashVideoPlayer : MonoBehaviour
{
    private VideoPlayer videoPlayer1;
    private VideoPlayer videoPlayer2;
    private RenderTexture renderTexture1;
    private RenderTexture renderTexture2;
    private Material skyboxMaterial;
    private float switchTimeBeforeEnd = 0f; // Default time in seconds before the video ends to switch
    private DashVideoMaster videoMaster; // Reference to the master script

    // UI Elements
    private MPDParser mpdParser;
    private SegmentFetcher segmentFetcher;
    private BufferManager bufferManager;
    private ABRAlgorithm abrAlgorithm;
    private MetricsLogger metricsLogger;
    private EyeQoEMetricsLogger eyeQoEMetricsLogger;
    private InputTracker inputTracker;
    private int segmentNumber = 1;
    private int videoIndex=0;

    private int startedTrackingeye = 0;
    public int qualityIndex = 0;
    private float starttemp =0;
    private float endtemp = 4;
    private int reburringCount=0;
    private bool isRebuffering = false;
    private float rebufferingTime = 0;
    private List<int> reburringIntervalIndexes = new List<int>();


    public void Initialize(Material skyboxMat, DashVideoMaster master)
    {
        skyboxMaterial = skyboxMat;
        videoMaster = master;

        // Create video players and render textures at runtime
        videoPlayer1 = gameObject.AddComponent<VideoPlayer>();
        videoPlayer2 = gameObject.AddComponent<VideoPlayer>();
        renderTexture1 = new RenderTexture(1920, 1080, 0);
        renderTexture2 = new RenderTexture(1920, 1080, 0);

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
        inputTracker = gameObject.AddComponent<InputTracker>();
        abrAlgorithm = new ABRAlgorithm();

        bufferManager.InitializeBufferQueue();
    }

    public void PlayVideo(string mpdURL, float switchTime=0.24f)
    {
        switchTimeBeforeEnd = switchTime;
        StartCoroutine(SetupVideoPlayer(mpdURL));
    }

    private IEnumerator SetupVideoPlayer(string mpdURL)
    {
        // Reinitialize the variables
        segmentNumber = 1;
        startedTrackingeye = 0;
        
        videoIndex = 0;
        starttemp = 0;
        endtemp = 4;

        videoPlayer1.Stop();
        videoPlayer2.Stop();

        videoPlayer1.targetTexture = renderTexture1;
        videoPlayer2.targetTexture = renderTexture2;
        skyboxMaterial.mainTexture = renderTexture1;

        yield return StartCoroutine(DeleteAllExistingVideos());

        string[] files = GetVideoFiles();
        Debug.Log("-->>>>> Files Found : " + files.Length + " All deleted, continuing process");
        yield return StartCoroutine(mpdParser.FetchMPD(mpdURL));
        var representations = mpdParser.GetRepresentations();
        segmentFetcher.fetchedAllSegments = false;
        Debug.Log("---->>>> debug 19 Quality Index: " + qualityIndex);
        abrAlgorithm.representationIndex = qualityIndex;
        segmentFetcher.streamURLPrefix = GetStreamURLPrefix(mpdURL);
        Debug.Log("---->>>> debug 19 Stream URL Prefix: " + segmentFetcher.streamURLPrefix);
        StartCoroutine(segmentFetcher.FetchSegments(representations, abrAlgorithm));

        setRandomRebufferingIntervals();
        // Prepare 2 video players in parallel
        StartCoroutine(PrepareSegment(videoPlayer1));
    }


    
    private IEnumerator PrepareSegment(VideoPlayer videoPlayer)
    {
        Debug.Log("---->>>> debug 19 Prepare segment function fired");
        int temp = 10;
        while (temp == 10)
        {
            string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
            if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
                filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");

            Debug.Log($" ----->>>> debug 19 Processing Video: {filePath}");
            metricsLogger.LogBufferStatus(segmentNumber);

            if (CheckIfFileExists(filePath))
            {
                Debug.Log("----->>>> debug 19 Prepared started for segment " + segmentNumber);
                videoPlayer.url = "file://" + filePath;
                videoPlayer.errorReceived += HandleVideoError;
                videoPlayer.prepareCompleted += PrepareCompleted;
                videoPlayer.Prepare();
                temp = 1;

                yield return null;
            }
            else if (segmentFetcher.fetchedAllSegments)
            {
                
                yield break;
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void PrepareCompleted(VideoPlayer videoPlayer)
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
        Debug.Log(" ---->>> debug 19 " + segmentNumber + " Video Prepared by player " + videoPlayerIndex);
        if (videoPlayer == videoPlayer1)
        {
            skyboxMaterial.mainTexture = renderTexture1;
        }
        else
        {
            skyboxMaterial.mainTexture = renderTexture2;
        }
        Debug.Log("---->>>> debug 19 Playing video: " + segmentNumber);
        CheckAndUpdateSwitchTime(videoPlayer);
        eyeQoEMetricsLogger.IncreaseSegmentNumber();
        videoPlayer.Play();
        if (startedTrackingeye == 0)
        {
            startedTrackingeye = 1;
            eyeQoEMetricsLogger.StartLoggingEverySecond();
        }

        videoPlayer.prepareCompleted -= PrepareCompleted;

        videoPlayer.loopPointReached += OnVideoEnded;
        ++segmentNumber;
        StartCoroutine(PrepareNextSegment(videoPlayer));
        // StartCoroutine(CheckIfPlayerIsPlayingAndNotify(videoPlayer));
    }

    private IEnumerator PrepareNextSegment(VideoPlayer videoPlayer)
    {
        Debug.Log("---->>>> debug 19 Prepare next segment function fired: " + segmentNumber);
        double timeRemaining = videoPlayer.length - videoPlayer.time;
        Debug.Log($"-->>>> debug 19  Time remaining: {timeRemaining} seconds");
        float rebufferTime = 0f;
        if(reburringIntervalIndexes.Contains(segmentNumber)){
            isRebuffering = true;
            rebufferTime = 3.0f;
            rebufferingTime = rebufferTime;
            Debug.Log("---->>>> debug 19 Rebuffering started for segment " + segmentNumber);
        }
        else{
            isRebuffering = false;
        }
        yield return new WaitForSeconds((float)videoPlayer.length - switchTimeBeforeEnd + rebufferTime);

        int temp = 100;
        while (temp == 100)
        {
            string nextFilePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
            if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
                nextFilePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");

            Debug.Log($"--->>>> debug 19 Processing Next Segment: {nextFilePath}");
            metricsLogger.LogBufferStatus(segmentNumber);

            if (CheckIfFileExists(nextFilePath))
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
            else if (segmentFetcher.fetchedAllSegments)
            {
                
                yield break;
            }
            else
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void OnVideoEnded(VideoPlayer videoPlayer)
    {
        ++videoIndex;
        inputTracker.SendImpulseAndTrackInput(videoIndex);
        Debug.Log("---->>>> debug 19 Video ended" + videoIndex);

        videoPlayer.loopPointReached -= OnVideoEnded;

        
    }
        private IEnumerator DeleteAllExistingVideos()
    {
        string[] files = GetVideoFiles();

        Debug.Log("-->>>>> Files Found : " + files.Length + " Deleting...");
        foreach (string file in files)
        {
            File.Delete(file);
        }

        yield return null;
    }

    private string[] GetVideoFiles()
    {
        if (GetPlatformCode() == 'L')
            return Directory.GetFiles("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "*.webm");
        if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
            return Directory.GetFiles(Application.persistentDataPath, "*.mp4");

        return new string[0];
    }

    private bool CheckIfFileExists(string filePath)
    {
        Debug.Log("----->>>>>> debug 19 Segment fetcher all fetched status : " + segmentFetcher.fetchedAllSegments);
        if (!File.Exists(filePath) && segmentFetcher.fetchedAllSegments)
        {
            StartCoroutine(EndPlayback());
        }
        return File.Exists(filePath);
    }
    private IEnumerator EndPlayback()
    {
        if(videoPlayer1.isPlaying || videoPlayer2.isPlaying){
            yield return new WaitForSeconds(0.1f);
        }
        else{
            Debug.Log("---->>>> debug 19 File does not exist and all segments have been fetched. Stopping the video player.");
            eyeQoEMetricsLogger.StopLogging();
            if(inputTracker.trackQueueSize > 0){
                Debug.Log("---->>>> debug 19 Waiting for input to log segment number " + segmentNumber);
                yield return new WaitForSeconds(0.1f);
            }
            videoMaster.OnVideoEnded();
            Cleanup();
            yield break;
        }
    }

    private void CheckAndUpdateSwitchTime(VideoPlayer videoPlayer){
        // if(videoIndex%3==0) {
        //     switchTimeBeforeEnd+=0.04f;
        // }
        // if(starttemp >= endtemp) return;
        // VideoPlayer prev = (videoPlayer == videoPlayer1) ? videoPlayer2 : videoPlayer1;
        // if(prev.isPlaying){
        //     Debug.Log(" ----->>>>> debug 19 previous Player is playing with time left : " + prev.time);
        //     float mid = starttemp + (endtemp - starttemp) / 2;
        //     starttemp = mid;
        //     switchTimeBeforeEnd = mid;
        //     Debug.Log("---->>>> debug 19 Switch time updated: " + mid); 
        // }
        // else{
        //     Debug.Log(" ---->>>> debug 19 previous Player is not playing, using end temp : " + endtemp);
        //     float mid = starttemp + (endtemp - starttemp) / 2;
        //     endtemp = mid;
        //     switchTimeBeforeEnd = mid;
        //     Debug.Log("---->>>> debug 19 Switch time updated: " + mid); 
        // }
    }

    void setRandomRebufferingIntervals(){
        reburringIntervalIndexes.Clear();
        var rand = new Random();

        if(qualityIndex == 0){
            reburringCount = rand.Next(3,5);
        }
        else if(qualityIndex == 1){
            reburringCount = rand.Next(2,5);
        }
        else{
            reburringCount = rand.Next(5,7);
        }
        Debug.Log("---->>>> debug 19 Rebuffering count: " + reburringCount);
        for(int i=0;i<reburringCount;i++){
            reburringIntervalIndexes.Add(rand.Next(1,15));
        }
    }

    void HandleVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError("---->>> debug 19 Error playing video: " + message);
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

    private IEnumerator CheckIfPlayerIsPlayingAndNotify(VideoPlayer videoPlayer)
    {
        while (!videoPlayer.isPlaying)
        {
            yield return new WaitForSeconds(0.1f);
        }
        eyeQoEMetricsLogger.IncreaseSegmentNumber();
    }

    private string GetStreamURLPrefix(string mpdURL)
    {
        string[] parts = mpdURL.Split('/');
        string temp = string.Join("/", parts, 0, parts.Length - 1);
        return temp + "/";
    }

    public void Cleanup()
    {
        // Cleanup all resources and destroy components
        Destroy(videoPlayer1);
        Destroy(videoPlayer2);
        Destroy(renderTexture1);
        Destroy(renderTexture2);
        Destroy(segmentFetcher);
        Destroy(metricsLogger);
        Destroy(eyeQoEMetricsLogger);
        Destroy(inputTracker);

        // Set non-UnityEngine.Object references to null
        mpdParser = null;
        abrAlgorithm = null;

        // Notify the master script that cleanup is complete
        videoMaster.OnCleanupComplete();
    }
}

