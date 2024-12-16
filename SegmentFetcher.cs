using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System;
using Random = System.Random;

public class SegmentFetcher : MonoBehaviour
{
    private BufferManager bufferManager;
    private MetricsLogger metricsLogger;
    public bool fetchedAllSegments = false;
    public string streamURLPrefix = "";
    private DQNAgent dqnAgent;  // DQNAgent for decision-making

// change randomizedSegments, buffersize.SetmaxBufferSize, others when trying new max buffer size.

    private const int RandomizedSegments = 2;  // Number of initial segments to randomize
    private int segmentNumber = 1;
    private string latencyLogFile = "";
    private float avgLatency = 0;
    private float count = 1.0f;
    private List<int> decisionArray = new List<int>();  // Array to store decisions per segment

    void Awake()
    {
        bufferManager = BufferManager.Instance;
        metricsLogger = gameObject.AddComponent<MetricsLogger>();
        latencyLogFile = Path.Combine(Application.persistentDataPath, "logLatency.txt");

        // Initialize DQNAgent (all parameters handled internally by the agent)
        dqnAgent = FindObjectOfType<DQNAgent>();

        // check if logLatency file exists, if yes add a new line with current base url
        if (!File.Exists(latencyLogFile))
        {
            
            File.AppendAllText(latencyLogFile, $"Base URL: {streamURLPrefix}\n");
        }
        
        // Set buffer size (N)
        bufferManager.SetBufferSize(2);  // Example buffer size, set it dynamically as needed
    }

    public IEnumerator FetchSegments(List<Representation> representations, int currentVideoID)
    {
        var Rand = new Random();
        Debug.Log("---->>>> debug 19 Fetching segments.");

        while (segmentNumber <= 1000) // Fetch up to 15 segments
        {
            // Check buffer size and download new only if buffer is under capacity (<N)
            if (bufferManager.GetBufferSize() < bufferManager.GetMaxBufferSize())
            {
                long startTimeLatency = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                int action;
                if (segmentNumber <= RandomizedSegments)
                {
                    // Randomized decision for the first X segments
                    action = Rand.Next(0, representations.Count);
                }
                else
                {
                    // For segments after the initial random choices, request a decision from DQNAgent
                                    action = dqnAgent.SelectAction(bufferManager.GetBufferSize());
                }

                decisionArray.Add(action);  // Store decision for the segment
                Debug.Log($"---->>>> Segment {segmentNumber} Decision: {action}");

                /*

                    for this experiment, we are only taking the first action (good quality) for each segment
                    Buffer size is considered
                */
                action = 0;

                Representation rep = representations[action];  // Select representation based on the action
                metricsLogger.LogRepresentation(rep.Id);
                metricsLogger.LogSegmentNumber(segmentNumber);

                string segmentUrl = streamURLPrefix + rep.Media.Replace("$RepresentationID$", rep.Id).Replace("$Number$", segmentNumber.ToString()).Replace(".mp4", (GetPlatformCode() == 'L' ? ".webm" : ".mp4"));

                bool success = false;
                float startTime = Time.time;
                yield return StartCoroutine(DownloadSegment(segmentUrl, segmentNumber, (result) => success = result));
                float endTime = Time.time;
                // Log the Arrival Time in Session{currentVideoID}ArrivalTime.txt

                string arrivalTimeTextFile = Path.Combine(Application.persistentDataPath, $"Session{currentVideoID}ArrivalTime.txt");
                File.AppendAllText(arrivalTimeTextFile, "Segemnt " + segmentNumber + " Arrival Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "\n");

                float downloadTime = endTime - startTime;
                avgLatency+=downloadTime;
                avgLatency/=count;
                count+=1.0f;
                File.AppendAllText(latencyLogFile, $"Segment {segmentNumber} Latency: {downloadTime} ms\n");
                File.AppendAllText(latencyLogFile, $"Average Latency: {avgLatency} ms\n");

                metricsLogger.LogDelay(downloadTime);

                try
                {
                    string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
                    if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
                    {
                        filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");
                    }

                    metricsLogger.LogNetworkSpeed((long)(new FileInfo(filePath).Length / downloadTime));
                    long bandwidth = (long)(new FileInfo(filePath).Length * 8 / 2);
                    Debug.Log("-->>>>> debug 19 Segment " + segmentNumber + " downloaded in " + downloadTime + " seconds. Bandwidth: " + bandwidth + " bits/s");
                    metricsLogger.LogBandwidth(bandwidth);
                }
                catch (System.Exception e)
                {
                    Debug.Log("Error: " + e.Message);
                }

                if (!success)
                {
                    Debug.Log("---->>>> Debug 19 No more segments to fetch. Stopping the fetch process.");
                    break;
                }

                segmentNumber++;
                yield return new WaitForSeconds(0.005f);  // Fetch the next segment immediately after finishing the current one
            }
            else
            {
                Debug.Log("Buffer is full. Waiting for space.");
                yield return new WaitForSeconds(1);  // Wait before checking buffer again
            }
        }

        
    }

    private IEnumerator DownloadSegment(string url, int segmentNumber, System.Action<bool> callback)
    {
        string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
        if (GetPlatformCode() == 'W' || GetPlatformCode() == 'A')
        {
            filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");
        }

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.responseCode == 404)
            {
                fetchedAllSegments = true;
                Debug.Log("---->>>> debug 19 Segment not found (404): " + url);
                metricsLogger.Log($"Segment not found (404): {url}");
                callback(false);
                yield break;
            }
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                fetchedAllSegments = true;
                Debug.LogError("Segment fetch error: " + webRequest.error);
                metricsLogger.Log($"Segment fetch error: {webRequest.error}");
                callback(false);

                yield break;
            }

            Debug.Log("---->>>> debug 19 Download function -> Segment " + segmentNumber + " downloaded.");
            Debug.Log("---->>>> debug 19 : Saving segment to: " + filePath);
            File.WriteAllBytes(filePath, webRequest.downloadHandler.data);
            bufferManager.AddToBuffer(filePath);
            callback(true);
        }
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
}

// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;
// using UnityEngine.Networking;
// using System;
// using Random = System.Random;

// public class SegmentFetcher : MonoBehaviour
// {
//     private BufferManager bufferManager;
//     private MetricsLogger metricsLogger;
//     public bool fetchedAllSegments = false;
//     public string streamURLPrefix = "";

//     private QTable qTable;

//     void Awake()
//     {
//         bufferManager = BufferManager.Instance;
//         metricsLogger = gameObject.AddComponent<MetricsLogger>();
//         qTable = QTable.Instance;
//     }

//     public IEnumerator FetchSegments(List<Representation> representations)
//     {  var Rand = new Random();
//         int segmentNumber = 1;
//         Debug.Log("---->>>> debug 19 Fetching segments.");
//         while (segmentNumber < 16) // Fetch 15 segments
//         {
//             long startTimeLatency = 0, endTimeLatency = 0;
//             startTimeLatency = DateTimeOffset.Now.ToUnixTimeMilliseconds();

//             Representation rep = representations[Rand.Next(0,representations.Count)];
//             // metricsLogger.LogBitrate(rep.Bitrate);
//             metricsLogger.LogRepresentation(rep.Id);
//             metricsLogger.LogSegmentNumber(segmentNumber);
//             endTimeLatency = DateTimeOffset.Now.ToUnixTimeMilliseconds();
//             Debug.Log("---->>>> debug 19 Latency: " + (endTimeLatency - startTimeLatency) + " ms");
//             try{
//                 int tep = qTable.MakeDecision();
//                 Debug.Log("---->>>> debug 19 QTable Decision: " + tep);
//             }
//             catch (System.Exception e)
//             {
//                 Debug.Log("Error: " + e.Message);
//             }
//             string segmentUrl = streamURLPrefix + rep.Media.Replace("$RepresentationID$", rep.Id).Replace("$Number$", segmentNumber.ToString()).Replace(".mp4", (GetPlatformCode() == 'L'? ".webm" : ".mp4"));
//             // Debug.Log("---->>>> debug 19 Fetching sample 500ms video from sampleVideos/video1.mp4");
//             // string sampleVideoUrl = streamURLPrefix + "sampleVideos/video1.mp4";
//             // float sample_startTime = Time.time;
//             // yield return StartCoroutine(DownloadSegment(sampleVideoUrl, segmentNumber, (result) => {}));
//             // float sample_endTime = Time.time;
//             // float sample_downloadTime = sample_endTime - sample_startTime;
//             // Debug.Log("---->>>> debug 19 Sample video downloaded in " + sample_downloadTime + " seconds.");
//             // Fetch the segment and add it to the buffer queue
//             bool success = false;
//             float startTime = Time.time;
//             yield return StartCoroutine(DownloadSegment(segmentUrl, segmentNumber, (result) => success = result));
//             float endTime = Time.time;

//             // Log the download duration and speed
//             float downloadTime = endTime - startTime;
//             metricsLogger.LogDelay(downloadTime);
//             try
//             {
//                 string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
//                 if(GetPlatformCode() == 'W' || GetPlatformCode() == 'A'){
//                     filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");
//                 }
//                 metricsLogger.LogNetworkSpeed((long)(new FileInfo(filePath).Length / downloadTime));
//                 long bandwidth = (long)(new FileInfo(filePath).Length * 8 / 2);
//                 Debug.Log("-->>>>> debug 19 Segment " + segmentNumber + " downloaded in " + downloadTime + " seconds. Bandwidth: " + bandwidth + " bits/s");
//                 metricsLogger.LogBandwidth(bandwidth);
//             }
//             catch (System.Exception e)
//             {
//                 Debug.Log("Error: " + e.Message);
//             }
            
            
            
//             if (!success)
//             {
//                 Debug.Log("---->>>> Debug 19 No more segments to fetch. Stopping the fetch process.");
//                 break;
//             }

//             segmentNumber++;
//             yield return new WaitForSeconds(0);  // Fetch the next segment 2 seconds after the current segment is finished fetching
//         }
//         if(segmentNumber == 16)
//         {
//             fetchedAllSegments = true;
//         }
//     }

//     private IEnumerator DownloadSegment(string url, int segmentNumber, System.Action<bool> callback)
//     {
        
//        string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
//        if(GetPlatformCode() == 'W' || GetPlatformCode() == 'A'){
//             filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");
//         }
//         float startTime = Time.time;
//         using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
//         {
//             yield return webRequest.SendWebRequest();
//             if (webRequest.responseCode == 404)
//             {
//                 fetchedAllSegments = true;
//                 Debug.Log("---->>>> debug 19 Segment not found (404): " + url);
//                 metricsLogger.Log($"Segment not found (404): {url}");
//                 callback(false);
//                 yield break;
//             }
//             if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
//             {
//                 Debug.LogError("Segment fetch error: " + webRequest.error);
//                 Debug.Log("---->>>> debug 19 Segment fetch error: " + webRequest.error);
//                 metricsLogger.Log($"Segment fetch error: {webRequest.error}");
//                 callback(false);
//                 yield break;
//             }
//             float endTime = Time.time;
//             float downloadTime = endTime - startTime;
//             Debug.Log("---->>>> debug 19 Download function -> Segment " + segmentNumber + " downloaded in " + downloadTime + " seconds.");

            
//             Debug.Log("---->>>> debug 19 : Saving segment to: " + filePath);
//             File.WriteAllBytes(filePath, webRequest.downloadHandler.data);
//             bufferManager.AddToBuffer(filePath);
//             callback(true);
//         }
//     }
//      public static char GetPlatformCode()
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

