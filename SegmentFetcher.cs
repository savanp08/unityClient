

using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class SegmentFetcher : MonoBehaviour
{
    private BufferManager bufferManager;
    private MetricsLogger metricsLogger;

    void Awake()
    {
        bufferManager = BufferManager.Instance;
        metricsLogger = gameObject.AddComponent<MetricsLogger>();
        
    }

    public IEnumerator FetchSegments(List<Representation> representations, ABRAlgorithm abrAlgorithm)
    {
        int segmentNumber = 1;
        while (segmentNumber < 40)
        {
            Representation rep = abrAlgorithm.GetBestRepresentation(representations);
            // metricsLogger.LogBitrate(rep.Bitrate);
            metricsLogger.LogRepresentation(rep.Id);
            metricsLogger.LogSegmentNumber(segmentNumber);

            string segmentUrl = "http://192.168.1.201:3000/playable_split/videos/" + rep.Media.Replace("$RepresentationID$", rep.Id).Replace("$Number$", segmentNumber.ToString()).Replace(".mp4", (GetPlatformCode() == 'L'? ".webm" : ".mp4"));
           
            // Fetch the segment and add it to the buffer queue
            bool success = false;
            float startTime = Time.time;
            yield return StartCoroutine(DownloadSegment(segmentUrl, segmentNumber, (result) => success = result));
            float endTime = Time.time;

            // Log the download duration and speed
            float downloadTime = endTime - startTime;
            metricsLogger.LogDelay(downloadTime);
            try
            {
                string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
                metricsLogger.LogNetworkSpeed((long)(new FileInfo(filePath).Length / downloadTime));
                long bandwidth = (long)(new FileInfo(filePath).Length * 8 / 2);
                Debug.Log("Segment " + segmentNumber + " downloaded in " + downloadTime + " seconds. Bandwidth: " + bandwidth + " bits/s");
                metricsLogger.LogBandwidth(bandwidth);
            }
            catch (System.Exception e)
            {
                Debug.Log("Error: " + e.Message);
            }
            
            
            
            if (!success)
            {
                Debug.Log("No more segments to fetch. Stopping the fetch process.");
                break;
            }

            segmentNumber++;
            yield return new WaitForSeconds(0);  // Fetch the next segment 2 seconds after the current segment is finished fetching
        }
    }

    private IEnumerator DownloadSegment(string url, int segmentNumber, System.Action<bool> callback)
    {
       string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
       if(GetPlatformCode() == 'W' || GetPlatformCode() == 'A'){
            filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");
        }

        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();
            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Segment fetch error: " + webRequest.error);
                metricsLogger.Log($"Segment fetch error: {webRequest.error}");
                callback(false);
                yield break;
            }
            if (webRequest.responseCode == 404)
            {
                Debug.Log("Segment not found (404): " + url);
                metricsLogger.Log($"Segment not found (404): {url}");
                callback(false);
                yield break;
            }
            Debug.Log("Saving segment to: " + filePath);
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

// public class SegmentFetcher : MonoBehaviour
// {
//     private BufferManager bufferManager;
//     private MetricsLogger metricsLogger;

//     void Awake()
//     {
//         bufferManager = BufferManager.Instance;
//         metricsLogger = gameObject.AddComponent<MetricsLogger>();
        
//     }

//     public IEnumerator FetchSegments(List<Representation> representations, ABRAlgorithm abrAlgorithm)
//     {
//         int segmentNumber = 1;
//         while (segmentNumber < 40)
//         {
//             Representation rep = abrAlgorithm.GetBestRepresentation(representations);
//             // metricsLogger.LogBitrate(rep.Bitrate);
//             metricsLogger.LogRepresentation(rep.Id);
//             metricsLogger.LogSegmentNumber(segmentNumber);

//             string segmentUrl = "http://192.168.1.201:3000/playable_split/videos/" + rep.Media.Replace("$RepresentationID$", rep.Id).Replace("$Number$", segmentNumber.ToString()).Replace(".mp4", (GetPlatformCode() == 'L'? ".webm" : ".mp4"));
           
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
//                 metricsLogger.LogNetworkSpeed((long)(new FileInfo(filePath).Length / downloadTime));
//                 long bandwidth = (long)(new FileInfo(filePath).Length * 8 / 2);
//                 Debug.Log("Segment " + segmentNumber + " downloaded in " + downloadTime + " seconds. Bandwidth: " + bandwidth + " bits/s");
//                 metricsLogger.LogBandwidth(bandwidth);
//             }
//             catch (System.Exception e)
//             {
//                 Debug.Log("Error: " + e.Message);
//             }
            
            
            
//             if (!success)
//             {
//                 Debug.Log("No more segments to fetch. Stopping the fetch process.");
//                 break;
//             }

//             segmentNumber++;
//             yield return new WaitForSeconds(0);  // Fetch the next segment 2 seconds after the current segment is finished fetching
//         }
//     }

//     private IEnumerator DownloadSegment(string url, int segmentNumber, System.Action<bool> callback)
//     {
//        string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
//        if(GetPlatformCode() == 'W' || GetPlatformCode() == 'A'){
//             filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");
//         }

//         using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
//         {
//             yield return webRequest.SendWebRequest();
//             if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
//             {
//                 Debug.LogError("Segment fetch error: " + webRequest.error);
//                 metricsLogger.Log($"Segment fetch error: {webRequest.error}");
//                 callback(false);
//                 yield break;
//             }
//             if (webRequest.responseCode == 404)
//             {
//                 Debug.Log("Segment not found (404): " + url);
//                 metricsLogger.Log($"Segment not found (404): {url}");
//                 callback(false);
//                 yield break;
//             }
//             Debug.Log("Saving segment to: " + filePath);
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
