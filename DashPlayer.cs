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
    private int lowestIndexAvaialable = 1;
    // UI Elements
    private MPDParser mpdParser;
    private SegmentFetcher segmentFetcher;
    private BufferManager bufferManager;
    private ABRAlgorithm abrAlgorithm;
    private MetricsLogger metricsLogger;
    private EyeQoEMetricsLogger eyeQoEMetricsLogger;
    private InputTracker inputTracker;
    public int segmentNumber = 1;
    private int videoIndex=0;
    private int currentVideoID = 0;

    private int startedTrackingeye = 0;
    public int qualityIndex = 0;
    private float starttemp =0;
    private float endtemp = 4;
    private int reburringCount=0;
    private float count_of_rebuufer_for_avg = 0.0f;
    private bool isRebuffering = false;
    private float rebufferingTime = 0;
    private List<int> reburringIntervalIndexes = new List<int>();
    public bool streamended = false;
    private string rebufferingLog = "";
    private int maxBufferCapacity = 2;


    public void Initialize(Material skyboxMat, DashVideoMaster master)
    {
        skyboxMaterial = skyboxMat;
        videoMaster = master;
        int height = 1080;
        int width = 1920;
        if(qualityIndex==1){
            width = 1536;
            height = 768;
        }
        
        else if(qualityIndex==2){
            width = 1024;
            height = 512;
        }
        
        // Create video players and render textures at runtime
        videoPlayer1 = gameObject.AddComponent<VideoPlayer>();
        videoPlayer2 = gameObject.AddComponent<VideoPlayer>();
        renderTexture1 = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);
        renderTexture2 = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32);

        videoPlayer1.targetTexture = renderTexture1;
        videoPlayer2.targetTexture = renderTexture2;
        skyboxMaterial.mainTexture = renderTexture1;

        videoPlayer1.playOnAwake = false;
        videoPlayer2.playOnAwake = false;
        videoPlayer1.isLooping = false;
        videoPlayer2.isLooping = false;

        // Test
        videoPlayer1.skipOnDrop = true;
        videoPlayer2.skipOnDrop = true;

        mpdParser = new MPDParser();
        segmentFetcher = gameObject.AddComponent<SegmentFetcher>();
        bufferManager = BufferManager.Instance;
        metricsLogger = gameObject.AddComponent<MetricsLogger>();
        eyeQoEMetricsLogger = gameObject.AddComponent<EyeQoEMetricsLogger>();
        inputTracker = gameObject.AddComponent<InputTracker>();
        abrAlgorithm = new ABRAlgorithm();
        
        rebufferingLog = Path.Combine(Application.persistentDataPath, "rebufferingLog.txt");
        
    }

    public void PlayVideo(string mpdURL, float switchTime=0.24f, int videoID=0)
    {
        switchTimeBeforeEnd = switchTime;
        currentVideoID = videoID;
        StartCoroutine(SetupVideoPlayer(mpdURL, videoID));
    }

    private IEnumerator SetupVideoPlayer(string mpdURL, int videoID)
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
        StartCoroutine(segmentFetcher.FetchSegments(representations, videoID));

        setRandomRebufferingIntervals();
        // Prepare 2 video players in parallel
        File.AppendAllText(rebufferingLog, "Starting new session with url : " + mpdURL + "\n");
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
                bufferManager.RemoveFromBuffer();
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
                rebufferingTime+=0.1f;
                count_of_rebuufer_for_avg+=1.0f;

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
                    bufferManager.RemoveFromBuffer();
                    videoPlayer2.prepareCompleted += PrepareCompleted;
                    videoPlayer2.Prepare();
                }
                else
                {
                    videoPlayer1.url = nextFilePath;
                    bufferManager.RemoveFromBuffer();
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
                // only count as rebuffer if no video is playing oin either players
                if(!videoPlayer1.isPlaying && !videoPlayer2.isPlaying)
                {
                rebufferTime+=0.1f;
                count_of_rebuufer_for_avg+=1.0f;
                File.AppendAllText(rebufferingLog, "Rebuffering at segment: " + segmentNumber + " Time: " + rebufferTime + "\n");
            File.AppendAllText(rebufferingLog, "Avg Rebuffering Time: " + rebufferTime/count_of_rebuufer_for_avg + "\n");
                }
                yield return new WaitForSeconds(0.1f);
            }
        }
    }

    private void OnVideoEnded(VideoPlayer videoPlayer)
{
    ++videoIndex;
    inputTracker.SendImpulseAndTrackInput(videoIndex);
    Debug.Log("---->>>> debug 19 Video ended, segment: " + (segmentNumber - 1));
    // log Finish time Timestamp (alomg with ms) in segmenth number +1 column in csv named Session{videoID}.csv
    File.AppendAllText(Path.Combine(Application.persistentDataPath, $"Session{currentVideoID}FinishTime.txt"), $"Segment {segmentNumber - 1} Finish Time: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff")}\n");
    File.AppendAllText(Path.Combine(Application.persistentDataPath, $"Session{currentVideoID}.csv"), $"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff")}, {videoPlayer.time}\n");
   
    videoPlayer.loopPointReached -= OnVideoEnded;

    // Construct the file path for the segment that just finished playing
    string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + (segmentNumber - 1) + ".webm");
    if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
        filePath = Path.Combine(Application.persistentDataPath, "segment" + (segmentNumber - 1) + ".mp4");
    
    
    

    // Delete the file from disk
    if (File.Exists(filePath))
    {
        File.Delete(filePath);
        Debug.Log("---->>>> debug 19 Video file deleted: " + filePath);
    }
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
        // using buffer manager to check if the file exists
        Debug.Log("---->>>> debug 19 Checking if file exists: " + filePath);
        Debug.Log("---->>>> debug 19 Buffer size: " + bufferManager.GetBufferSize());
        bufferManager.printQueue();
        if(bufferManager.GetBufferSize() ==0 && segmentFetcher.fetchedAllSegments){
            Debug.Log("---->>>> debug 19 Buffer is empty and all segments have been fetched. Exiting dash player.");
            StartCoroutine(EndPlayback());
            
        }
        Debug.Log("----->>>>>> debug 19 Segment fetcher all fetched status : " + segmentFetcher.fetchedAllSegments);
        if(File.Exists(filePath) && bufferManager.isSegmentFirstInBuffer(filePath)){
            Debug.Log("---->>>> debug 19 File exists and is first in buffer: " + filePath);
        }
        return File.Exists(filePath) && bufferManager.isSegmentFirstInBuffer(filePath);
    }
    private IEnumerator EndPlayback()
    {
        while(videoPlayer1.isPlaying || videoPlayer2.isPlaying){
            Debug.Log("---->>>> debug 19 File does not exist but video is playing. Waiting for video to end.");
            yield return new WaitForSeconds(0.1f);
        }
        streamended = true;
            Debug.Log("---->>>> debug 19 test 1 File does not exist and all segments have been fetched. Stopping the video player.");
            Debug.Log("---->>>> debug 19 input queue : "+ inputTracker.trackQueueSize);
        if(segmentFetcher.fetchedAllSegments){
        videoMaster.OnVideoEnded();
        Cleanup();
        yield break;
        }
            
            // while(inputTracker.trackQueueSize > 0){
            //      string nextFilePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
            // if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
            //     nextFilePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");
            //     if(segmentFetcher.fetchedAllSegments){
            //         Debug.Log("---->>>> debug 19 All inputs logged. exiting dash player.");
            //         videoMaster.OnVideoEnded();
            //         Cleanup();
            //         yield break;
            //     } 
            //     Debug.Log("---->>>> debug 19 Waiting for input to log segment number " + segmentNumber);
                
            // }
            
        
    }
    public void EndPlaybackAndCleanup()
    {
                    Debug.Log("---->>>> debug 19 EndPlaybackAndCleanup function fired");
                    Debug.Log("---->>>> debug 19 input queue : "+ inputTracker.trackQueueSize);
                    Debug.Log("---->>>> debug 19 Stream ended : "+ streamended);

        if(streamended){
            Debug.Log("---->>>> debug 19 Stream has already ended. Calling the EndPlayback function.");
            StartCoroutine(EndPlayback());
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
            reburringCount = rand.Next(0,1);
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

    private void deleteLowest(){
        string path = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + lowestIndexAvaialable + ".webm");
        if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
            path = Path.Combine(Application.persistentDataPath, "segment" + lowestIndexAvaialable + ".mp4");
        
        while(segmentNumber - lowestIndexAvaialable>  maxBufferCapacity){
            if(File.Exists(path)){
                File.Delete(path);
                lowestIndexAvaialable++;
            }
            else{
                break;
            }
        
        }

    }

    public void Cleanup()
    {
        // Cleanup all resources and destroy components
        Debug.Log("---->>>> XXXXXXXX ->>> debug 19 Cleaning up DashVideoPlayer");
        eyeQoEMetricsLogger.StopLogging();
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

