
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;
// using UnityEngine.Video;

// public class DashVideoPlayer : MonoBehaviour
// {
//     public string mpdURL = "http://192.168.1.201:3000/playable_split/videos/Manifest.mpd"; // URL to the MPD file
//     private VideoPlayer videoPlayer1;
//     private VideoPlayer videoPlayer2;
//     private MPDParser mpdParser;
//     private SegmentFetcher segmentFetcher;
//     private BufferManager bufferManager;
//     private ABRAlgorithm abrAlgorithm;
//     private MetricsLogger metricsLogger;
//     private EyeQoEMetricsLogger eyeQoEMetricsLogger;
//     private bool isPreparingNextSegment1 = false;
//     private bool isPreparingNextSegment2 = false;
//     public int segmentNumber = 1;
//     public RenderTexture renderTexture1;
//     public RenderTexture renderTexture2;
//     public Material skyboxMaterial;
//     public float switchTimeBeforeEnd = 0f; // Time in seconds before the video ends to switch

//     private Dictionary<VideoPlayer, RenderTexture> videoPlayerRenderTextureMap;

//     void Start()
//     {
//         videoPlayer1 = gameObject.AddComponent<VideoPlayer>();
//         videoPlayer2 = gameObject.AddComponent<VideoPlayer>();

//         videoPlayer1.targetTexture = renderTexture1;
//         videoPlayer2.targetTexture = renderTexture2;

//         skyboxMaterial.mainTexture = renderTexture1;

//         videoPlayerRenderTextureMap = new Dictionary<VideoPlayer, RenderTexture>
//         {
//             { videoPlayer1, renderTexture1 },
//             { videoPlayer2, renderTexture2 }
//         };

//         mpdParser = new MPDParser();
//         segmentFetcher = gameObject.AddComponent<SegmentFetcher>();
//         bufferManager = BufferManager.Instance;
//         metricsLogger = gameObject.AddComponent<MetricsLogger>();
//         eyeQoEMetricsLogger = gameObject.AddComponent<EyeQoEMetricsLogger>();

//         abrAlgorithm = new ABRAlgorithm();

//         bufferManager.InitializeBufferQueue();
//         StartCoroutine(SetupVideoPlayer());
//     }

//     IEnumerator SetupVideoPlayer()
//     {
//         yield return StartCoroutine(mpdParser.FetchMPD(mpdURL));
//         var representations = mpdParser.GetRepresentations();
//         StartCoroutine(segmentFetcher.FetchSegments(representations, abrAlgorithm));

//         // Prepare 2 videoPlayers in parallel
//         StartCoroutine(PrepareSegment(videoPlayer1));
//         StartCoroutine(PrepareSegment(videoPlayer2));
//     }

//     IEnumerator PrepareSegment(VideoPlayer videoPlayer)
//     {
//         int tempNumber = segmentNumber;
//         if (videoPlayer == videoPlayer2) tempNumber++;
//         bool flag = false;
//         if (videoPlayer == videoPlayer1) flag = isPreparingNextSegment1;
//         else if (videoPlayer == videoPlayer2) flag = isPreparingNextSegment2;
//         int videoPlayerIndex = videoPlayer == videoPlayer1 ? 1 : 2;
//         Debug.Log("Checking if Prepare is necessary for segment " + tempNumber + " in player " + videoPlayerIndex);
//         while (!flag)
//         {
//             string filePath = Path.Combine(Application.persistentDataPath, "segment" + tempNumber + ".mp4");

//             if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath)) // Ensure the file exists
//             {
//                 metricsLogger.LogBufferStatus(tempNumber);
//                 videoPlayer.url = "file://" + filePath;
//                 videoPlayer.errorReceived += HandleVideoError;

//                 // If the video is prepared, call the videoPrepared method which plays the video if the other videoPlayer is f seconds away from ending
//                 videoPlayer.prepareCompleted += VideoPrepared;
//                 Debug.Log("Trying to prepare segment: " + filePath);
//                 videoPlayer.Prepare();
//                 if (videoPlayer == videoPlayer1)
//                 {
//                     isPreparingNextSegment1 = true;
//                 }
//                 else
//                 {
//                     isPreparingNextSegment2 = true;
//                 }
//                 flag = true;
//                 yield break;
//                 // Wait until the video is prepared before playing it
//             }
//             else
//             {
//                 yield return new WaitForSeconds(1); // Retry after a short delay
//             }
//         }
//     }

//     void VideoPrepared(VideoPlayer videoPlayer)
//     {
//         int videoPlayerIndex = videoPlayer == videoPlayer1? 1: 2;
//         Debug.Log("Video Prepared by player "+ videoPlayerIndex);
//         if (videoPlayer == videoPlayer1)
//         {
//             SwitchToVideoPlayer(videoPlayer2, videoPlayer1, renderTexture1);
//         }
//         else
//         {
//             SwitchToVideoPlayer(videoPlayer1, videoPlayer2, renderTexture2);
//         }
//     }

// public void SwitchToVideoPlayer(VideoPlayer currentPlayer, VideoPlayer otherPlayer, RenderTexture renderTexture)
// {
//     StartCoroutine(SwitchVideoCoroutine(currentPlayer, otherPlayer, renderTexture));
// }

// private IEnumerator SwitchVideoCoroutine(VideoPlayer currentPlayer, VideoPlayer otherPlayer, RenderTexture renderTexture)
// {
//     int videoPlayerIndex = currentPlayer == videoPlayer1 ? 1 : 2;
//     int breakIndex = 0;
//     Debug.Log("current player " + videoPlayerIndex + " called");

    
//         breakIndex++;
//         double currentSegmentLength = currentPlayer.length;
//         double time = currentPlayer.time;
//         double timeLeft = currentSegmentLength - time;

//         Debug.Log("Time left for current player: " + timeLeft);

//         double otherSegmentLength = otherPlayer.length;
//         double otherTime = otherPlayer.time;
//         double otherTimeLeft = otherSegmentLength - otherTime;

//         Debug.Log("Time left for other player: " + otherTimeLeft);
//         if(timeLeft > switchTimeBeforeEnd) {
//             Debug.Log("Waiting 0.1 seconds to check again...");
//             yield return new WaitForSeconds(0.1f);
//         }
        

//         // If the time to switch is less than x and this videoPlayer is not playing (not checking this might overwrite the current playback) then play the prepared segment
//         if (timeLeft <= switchTimeBeforeEnd && !currentPlayer.isPlaying)
//         {
//             Debug.Log("Starting new Playback in " + ((videoPlayerIndex+1)%2) + " at time " + time + " with time left " + timeLeft + " and switch time before end " + switchTimeBeforeEnd);
//             Debug.Log("Time left for current player: " + timeLeft);
//             Debug.Log("Time left for other player: " + otherTimeLeft);

//             otherPlayer.Play();
//             // Set isPreparingNextSegment to false so that the next segment is prepared in the current player
//             if (otherPlayer == videoPlayer1)
//             {
//                 isPreparingNextSegment1 = false;
//             }
//             else
//             {
//                 isPreparingNextSegment2 = false;
//             }
//         }
//         else if (currentPlayer.isPlaying)
//         {
//             yield return new WaitForSeconds(0.1f);
           
//         }

//         // If the videoPlayer starts playing (using the above If statement) and the other videoPlayer has ended playback, then switch the skybox to the current videoPlayer
//         // switching the render texture after x seconds but starting the playback before x seconds of the previous segment playback ends will ensure that the next video is mounted 
//         // and this switch delay is not displayed and overlaps with the x seconds of the previous playback
//         if (otherPlayer.isPlaying && !currentPlayer.isPlaying)
//         {
//             Debug.Log("Switching skybox to " + ((videoPlayerIndex+1)%2) + " at time " + time + " with time left " + timeLeft + " and switch time before end " + switchTimeBeforeEnd);
//             if(currentPlayer == videoPlayer1) {
//                 skyboxMaterial.mainTexture = renderTexture1;
//             } else {
//                 skyboxMaterial.mainTexture = renderTexture2;
//             }
//             yield break;
//         }

//         yield return null; // Yield for a frame to avoid blocking the main thread
    
// }


    // public void SwitchToVideoPlayer(VideoPlayer currentPlayer, VideoPlayer otherPlayer, RenderTexture renderTexture)
    // {
        
    //     int videoPlayerIndex = currentPlayer == videoPlayer1 ? 1 : 2;
    //     int breakIndex = 0;
    //     Debug.Log("current player "+ videoPlayerIndex + " called" );

    //     while (true && breakIndex < 100)
    //     {
    //         breakIndex++;
    //         double currentSegmentLength = currentPlayer.length;
    //         double time = currentPlayer.time;
    //         double timeLeft = currentSegmentLength - time;

    //         Debug.Log("Time left for current player: " + timeLeft);

    //         double othersegmentLength = otherPlayer.length;
    //         double othertime = otherPlayer.time;
    //         double otherTimeLeft = othersegmentLength - othertime;

    //         Debug.Log("Time left for other player: " + otherTimeLeft);

    //        if(timeLeft > switchTimeBeforeEnd) {
    //         new WaitForSeconds(0.1f);
    //         continue;
    //        }
    //         // If the time to switch is less than x and this videoPlayer is not playing (not checking this might overwrite the current playback) then play the prepared segment
    //         if (timeLeft <= switchTimeBeforeEnd && !currentPlayer.isPlaying)
    //         {
    //             Debug.Log("Starting new Playback in "+ videoPlayerIndex + " at time " + time + " with time left " + timeLeft + " and switch time before end " + switchTimeBeforeEnd);

    //             otherPlayer.Play();
    //             // Set isPreparingNextSegment to false so that the next segment is prepared in the current player
    //             if (otherPlayer == videoPlayer1)
    //             {
    //                 isPreparingNextSegment1 = false;
    //             }
    //             else
    //             {
    //                 isPreparingNextSegment2 = false;
    //             }
    //         }
    //         else if(currentPlayer.isPlaying)  {
    //             new WaitForSeconds(0.1f);
    //             continue;
    //         } // Wait for 100ms before checking again

    //         // If the videoPlayer starts playing (using the above If statement) and the other videoPlayer has ended playback, then switch the skybox to the current videoPlayer
    //         // switching the render texture after x seconds but starting the playback before x seconds of the previous segment playback ends will ensure that the next video is mounted 
    //         // and this switch delay is not displayed and overlaps with the x seconds of the previous playback
    //         if (otherPlayer.isPlaying && !currentPlayer.isPlaying)
    //         {
    //             Debug.Log("Switching skybox to " + videoPlayerIndex + " at time " + time + " with time left " + timeLeft + " and switch time before end " + switchTimeBeforeEnd);
    //             skyboxMaterial.mainTexture = renderTexture;
    //             break;
    //         }
    //     }
    // }

//     public void HandleVideoError(VideoPlayer vp, string message)
//     {
//         Debug.LogError("Error playing video: " + message);
//         metricsLogger.Log($"Error: {message}");
//     }
// }


// custom code (to remove switch delay) with syntax error, try later 
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Video;

// public class DashVideoPlayer : MonoBehaviour
// {
//     public string mpdURL = "http://192.168.1.201:3000/playable_split/videos/Manifest.mpd"; // URL to the MPD file
//     private VideoPlayer videoPlayer1;
//     private VideoPlayer videoPlayer2;
//     private MPDParser mpdParser;
//     private SegmentFetcher segmentFetcher;
//     private BufferManager bufferManager;
//     private ABRAlgorithm abrAlgorithm;
//     private MetricsLogger metricsLogger;
//     private EyeQoEMetricsLogger eyeQoEMetricsLogger;
//     private bool isPreparingNextSegment = false;
//     public int segmentNumber = 1;
//     public int isPreparingNextSegment1 = false;
//     public int isPreparingNextSegment2 = false;
//     public RenderTexture renderTexture1;
//     public RenderTexture renderTexture2;
//     public Material skyboxMaterial;
//     public float switchTimeBeforeEnd = 0f; // Time in seconds before the video ends to switch

//     private Dictionary<VideoPlayer, RenderTexture> videoPlayerRenderTextureMap;

//     void Start()
//     {
//         videoPlayer1 = gameObject.AddComponent<VideoPlayer>();
//         videoPlayer2 = gameObject.AddComponent<VideoPlayer>();

//         videoPlayer1.targetTexture = renderTexture1;
//         videoPlayer2.targetTexture = renderTexture2;

//         skyboxMaterial.mainTexture = renderTexture1;

//         videoPlayerRenderTextureMap = new Dictionary<VideoPlayer, RenderTexture>
//         {
//             { videoPlayer1, renderTexture1 },
//             { videoPlayer2, renderTexture2 }
//         };

//         mpdParser = new MPDParser();
//         segmentFetcher = gameObject.AddComponent<SegmentFetcher>();
//         bufferManager = BufferManager.Instance;
//         metricsLogger = gameObject.AddComponent<MetricsLogger>();
//         eyeQoEMetricsLogger = gameObject.AddComponent<EyeQoEMetricsLogger>();

//         abrAlgorithm = new ABRAlgorithm();

//         bufferManager.InitializeBufferQueue();
//         StartCoroutine(SetupVideoPlayer());
//     }

//     IEnumerator SetupVideoPlayer()
//     {
//         yield return StartCoroutine(mpdParser.FetchMPD(mpdURL));
//         var representations = mpdParser.GetRepresentations();
//         StartCoroutine(segmentFetcher.FetchSegments(representations, abrAlgorithm));

//         // Prepare 2 videoPlayers parallely
//         StartCoroutine(PrepareSegement(videoPlayer1));
//         StartCoroutine(PrepareSegement(videoPlayer2));
//     }

//     IEnumerator PrepareSegment(VideoPlayer videoPlayer)
//     {
//         int tempNumber = segmentNumber;
//         if(videoPlayer == videoPlayer2) tempNumber++;
//         bool flag = false;
//         if(videoPlayer == videoPlayer1) flag = isPreparingNextSegment1;
//         else if(videoPlayer == videoPlayer2) flag = isPreparingNextSegment2;
//         while (true && !flag)
//         {
//             string filePath = Path.Combine(Application.persistentDataPath, "segment" + tempNumber + ".mp4");
            
           

//             if (!string.IsNullOrEmpty(filePath))
//             {
//                 metricsLogger.LogBufferStatus(tempNumber);
//                 videoPlayer.url = "file://" + filePath;
//                 videoPlayer.errorReceived += HandleVideoError;

//                 // If the video is Prepared, call the videoPrepared method which plays the video if the other videoPlayer is f seconds away from ending
//                 videoPlayer.prepareCompleted +=  VideoPrepared;
//                 Debug.Log("Trying to prepare segment: " + filePath);
//                 videoPlayer.Prepare();
//                 if (videoPlayer == videoPlayer1)
//                 {
//                     isPreparingNextSegment1 = true;
//                     flag = true;
//                 }
//                 else
//                 {
//                     isPreparingNextSegment2 = true;
//                     flag = true;
//                 }
//                 // Wait until the video is prepared before playing it
//             }
//             else
//             {

//                 yield return null;
//             }
//         }
//     }

//     private void VideoPrepared(VideoPlayer videoPlayer)
//     {
        
//        while(videoPlayer == videoPlayer1){
//         double currentSegmentLength = videoPlayer2.length;
//         double time = videoPlayer2.time;
//         double timeLeft = currentSegmentLength - time;

//         // If the time to switch is less than x and this videoPlayer is not playing (not checking this might overwrite the current playback) then play the prepared segment
//         if(timeLeft <= switchTimeBeforeEnd && !videoPlayer1.isPlaying){
//             videoPlayer1.Play();
//             // Set isPreparingNextSegment1 to false so that the next segment is prepared in videoPlayer1
//             isPreparingNextSegment1 = false;
//         }

//         // if the videoPlayer starts playing (using the above If statement) and the other videoPlayer has ended playback, then switch the skybox to the current videoPlayer.
//         // switching the render texture after x seconds but starting the playback before  xseconds of previous segment playback ends will ensure that the next video is mounted 
//         // and this switch delay is not displayed and overlaps with the x seconds of the previous playback
//         if(!videoPlayer2.isPlaying && videoPlayer1.isPlaying){
//             skyboxMaterial.mainTexture = renderTexture1;
//             yield break;
//         }
//        }
//          while(videoPlayer == videoPlayer2){
//             double currentSegmentLength = videoPlayer1.length;
//         double time = videoPlayer1.time;
//         double timeLeft = currentSegmentLength - time;

//         // If the time to switch is less than x and this videoPlayer is not playing (not checking this might overwrite the current playback) then play the prepared segment
//         if(timeLeft <= switchTimeBeforeEnd && !videoPlayer2.isPlaying){
//             videoPlayer2.Play();
//             // Set isPreparingNextSegment2 to false so that the next segment is prepared in videoPlayer2
//             isPreparingNextSegment2 = false;
//         }

//         // if the videoPlayer starts playing (using the above If statement) and the other videoPlayer has ended playback, then switch the skybox to the current videoPlayer.
//         // switching the render texture after x seconds but starting the playback before  xseconds of previous segment playback ends will ensure that the next video is mounted 
//         // and this switch delay is not displayed and overlaps with the x seconds of the previous playback
        
//         if(!videoPlayer1.isPlaying && videoPlayer2.isPlaying){
//             skyboxMaterial.mainTexture = renderTexture2;
//             yield break;
//         }
//         }
//         yield return null;
//     }
    

//     public void HandleVideoError(VideoPlayer vp, string message)
//     {
//         Debug.LogError("Error playing video: " + message);
//         metricsLogger.Log($"Error: {message}");
//     }

// }


// using System.Collections;
// using UnityEngine;
// using UnityEngine.Video;

// public class DashVideoPlayer : MonoBehaviour
// {
//     public string mpdURL = "http://192.168.1.201:3000/playable_split/videos/Manifest.mpd"; // URL to the MPD file
//     private VideoPlayer videoPlayer1;
//     private VideoPlayer videoPlayer2;
//     private MPDParser mpdParser;
//     private SegmentFetcher segmentFetcher;
//     private BufferManager bufferManager;
//     private ABRAlgorithm abrAlgorithm;
//     private MetricsLogger metricsLogger;
//     private EyeQoEMetricsLogger eyeQoEMetricsLogger;
//     private bool isPreparingNextSegment = false;
//     public int segmentNumber = 1;
//     public RenderTexture renderTexture1;
//     public RenderTexture renderTexture2;
//     public Material skyboxMaterial;
//     public float switchTimeBeforeEnd = 0f; // Time in seconds before the video ends to switch

//     void Start()
//     {
//         videoPlayer1 = gameObject.AddComponent<VideoPlayer>();
//         videoPlayer2 = gameObject.AddComponent<VideoPlayer>();

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
//         StartCoroutine(SetupVideoPlayer());
//     }

//     IEnumerator SetupVideoPlayer()
//     {
//         yield return StartCoroutine(mpdParser.FetchMPD(mpdURL));
//         var representations = mpdParser.GetRepresentations();
//         StartCoroutine(segmentFetcher.FetchSegments(representations, abrAlgorithm));
//         StartCoroutine(PlayVideoSegments(videoPlayer1, renderTexture1));
//     }

//     IEnumerator PlayVideoSegments(VideoPlayer videoPlayer, RenderTexture renderTexture)
//     {
//         while (true)
//         {
//             string filePath = bufferManager.GetNextSegment();
//             metricsLogger.LogBufferStatus(segmentNumber);
//             Debug.Log("Trying to play segment: " + filePath);

//             if (!string.IsNullOrEmpty(filePath))
//             {
//                 videoPlayer.url = "file://" + filePath;
//                 videoPlayer.errorReceived += HandleVideoError;
//                 videoPlayer.prepareCompleted += (VideoPlayer vp) => videoPrepared(vp, renderTexture);
//                 videoPlayer.Prepare();

//                 // Wait until the video is prepared before playing it
//                 while (!videoPlayer.isPrepared)
//                 {
//                     yield return null;
//                 }
                

//         }
//     }
//     }

//     public void videoPrepared(VideoPlayer videoPlayer, RenderTexture renderTexture)
//     {
//         Debug.Log("Video prepared. Playing..."+ segmentNumber);
//         if(videoPlayer == videoPlayer1){
//             skyboxMaterial.mainTexture = renderTexture1;
            
//         }
//         else{
//             skyboxMaterial.mainTexture = renderTexture2;
           
//         }
//         videoPlayer.Play();
//         videoPlayer.prepareCompleted -= videoPrepared;

//         videoPlayer.loopPointReached += OnVideoEnded;

//         // schedule the preparation of the next segment
//         StartCoroutine(PrepareNextSegment(videoPlayer));
        
//     }

//     IEnumerator PrepareNextSegment(VideoPlayer currentVideoPlayer)
//     {
//         yield return new WaitForSeconds((float)currentVideoPlayer.length - switchTimeBeforeEnd);
//         segmentNumber++;

//         string nextFilePath = bufferManager.GetNextSegment();
//         if (string.IsNullOrEmpty(nextFilePath))
//         {
//             isPreparingNextSegment = false;
//             yield break;
//         }
        
//         if (currentVideoPlayer == videoPlayer1)
//         {
//             videoPlayer2.url = "file://" + nextFilePath;
//             videoPlayer2.prepareCompleted += (VideoPlayer vp) => videoPrepared(vp, renderTexture2);
//             videoPlayer2.Prepare();
//         }
//         else
//         {
//             videoPlayer1.url = "file://" + nextFilePath;
//             videoPlayer1.prepareCompleted += (VideoPlayer vp) => videoPrepared(vp, renderTexture1);
//             videoPlayer1.Prepare();
//         }

//         isPreparingNextSegment = false;
//     }

//     public void OnVideoEnded(VideoPlayer vp)
//     {
//         Debug.Log("Video ended");
//         videoPlayer1.loopPointReached -= OnVideoEnded;
//     }

//     void HandleVideoError(VideoPlayer vp, string message)
//     {
//         Debug.LogError("Error playing video: " + message);
//         metricsLogger.Log($"Error: {message}");
//     }
// }


// using System.Collections;
// using UnityEngine;
// using UnityEngine.Video;

// public class DashVideoPlayer : MonoBehaviour
// {
//     public string mpdURL = "http://192.168.1.201:3000/playable_split/videos/Manifest.mpd"; // URL to the MPD file
//     private VideoPlayer videoPlayer;
//     private MPDParser mpdParser;
//     private SegmentFetcher segmentFetcher;
//     private BufferManager bufferManager;
//     private ABRAlgorithm abrAlgorithm;
//     private MetricsLogger metricsLogger;
//     private EyeQoEMetricsLogger eyeQoEMetricsLogger;
//     private bool isPreparingNextSegment = false;
//     public int segmentNumber = 1;

//     void Start()
//     {
//         videoPlayer = GetComponent<VideoPlayer>();
//         mpdParser = new MPDParser();
//         segmentFetcher = gameObject.AddComponent<SegmentFetcher>();
//         bufferManager = BufferManager.Instance;
//         metricsLogger = gameObject.AddComponent<MetricsLogger>();
//         eyeQoEMetricsLogger = gameObject.AddComponent<EyeQoEMetricsLogger>();

//         abrAlgorithm = new ABRAlgorithm();

//         bufferManager.InitializeBufferQueue();
//         StartCoroutine(SetupVideoPlayer());
//     }

//     IEnumerator SetupVideoPlayer()
//     {
//         yield return StartCoroutine(mpdParser.FetchMPD(mpdURL));
//         var representations = mpdParser.GetRepresentations();
//         StartCoroutine(segmentFetcher.FetchSegments(representations, abrAlgorithm));
//         StartCoroutine(PlayVideoSegments());
//     }

//     IEnumerator PlayVideoSegments()
//     {
//         while (true)
//         {
//             string filePath = bufferManager.GetNextSegment();
//             metricsLogger.LogBufferStatus(segmentNumber);
//             Debug.Log("Trying to play segment: " + filePath);

//             if (!string.IsNullOrEmpty(filePath))
//             {
//                 videoPlayer.url = "file://" + filePath;
//                 videoPlayer.errorReceived += HandleVideoError;
//                 videoPlayer.prepareCompleted += PrepareCompleted;
//                 videoPlayer.Prepare();

//                 // Wait until the video is prepared before playing it
//                 while (!videoPlayer.isPrepared)
//                 {
//                     yield return null;
//                 }
//                 videoPlayer.Play();
//                 segmentNumber++;
//                 metricsLogger.Log("Video started playing");

//                 // While playing the current segment, prepare the next segment
//                 if (!isPreparingNextSegment)
//                 {
//                     isPreparingNextSegment = true;
//                     StartCoroutine(PrepareNextSegment());
//                 }

//                 // Wait until the video finishes playing
//                 while (videoPlayer.isPlaying)
//                 {
//                     eyeQoEMetricsLogger.CollectAndLogEyeData();
//                     yield return null;
//                 }

//                 metricsLogger.Log("Video finished playing");
//             }
//             else
//             {
//                 yield return null;
//             }
//         }
//     }

//     IEnumerator PrepareNextSegment()
//     {
//         // Fetch the next segment every 2 seconds to keep the buffer ready
//         yield return new WaitForSeconds(2);
//         isPreparingNextSegment = false;
//     }

//     void PrepareCompleted(VideoPlayer vp)
//     {
//         Debug.Log("Video prepared. Playing...");
//         vp.Play();
//     }

//     void HandleVideoError(VideoPlayer vp, string message)
//     {
//         Debug.LogError("Error playing video: " + message);
//         metricsLogger.Log($"Error: {message}");
//     }
// }


// using System.Collections;
// using UnityEngine;
// using UnityEngine.Video;

// public class DashVideoPlayer : MonoBehaviour
// {
//     public string mpdURL = "http://192.168.1.201:3000/playable_split/videos/Manifest.mpd"; // URL to the MPD file
//     private VideoPlayer videoPlayer;
//     private MPDParser mpdParser;
//     private SegmentFetcher segmentFetcher;
//     private BufferManager bufferManager;
//     private ABRAlgorithm abrAlgorithm;
//     private bool isPreparingNextSegment = false;
//     private MetricsLogger metricsLogger;
//     public int segmentNumber=1;

//     void Start()
//     {
//         videoPlayer = GetComponent<VideoPlayer>();
//         mpdParser = new MPDParser();
//         segmentFetcher = gameObject.AddComponent<SegmentFetcher>();
//         bufferManager = BufferManager.Instance;
//         metricsLogger = gameObject.AddComponent<MetricsLogger>();

//         abrAlgorithm = new ABRAlgorithm();

//         bufferManager.InitializeBufferQueue();
//         StartCoroutine(SetupVideoPlayer());
//     }

//     IEnumerator SetupVideoPlayer()
//     {
//         yield return StartCoroutine(mpdParser.FetchMPD(mpdURL));
//         var representations = mpdParser.GetRepresentations();
//         StartCoroutine(segmentFetcher.FetchSegments(representations, abrAlgorithm));
//         StartCoroutine(PlayVideoSegments());
//     }

//     IEnumerator PlayVideoSegments()
//     {
//         while (true)
//         {
//             string filePath = bufferManager.GetNextSegment();
//             metricsLogger.LogBufferStatus(segmentNumber);
//             Debug.Log("trying Playing segment: " + filePath);
//             if (!string.IsNullOrEmpty(filePath))
//             {
//                 videoPlayer.url = "file://" + filePath;
//                 videoPlayer.errorReceived += HandleVideoError;
//                 videoPlayer.prepareCompleted += PrepareCompleted;
//                 videoPlayer.Prepare();

//                 // Wait until the video is prepared before playing it
//                 while (!videoPlayer.isPrepared)
//                 {
//                     yield return null;
//                 }
//                 videoPlayer.Play();
//                 segmentNumber++;
//                 // Log the playback start
//                 metricsLogger.Log("Video started playing");

//                 // While playing the current segment, prepare the next segment
//                 if (!isPreparingNextSegment)
//                 {
//                     isPreparingNextSegment = true;
//                     StartCoroutine(PrepareNextSegment());
//                 }

//                 // Wait until the video finishes playing
//                 while (videoPlayer.isPlaying)
//                 {
//                     yield return null;
//                 }

//                 // Log the playback end
//                 metricsLogger.Log("Video finished playing");
//             }
//             else
//             {
//                 yield return null;
//             }
//         }
//     }

//     IEnumerator PrepareNextSegment()
//     {
//         // Fetch the next segment every 1 seconds to keep the buffer ready
//         yield return new WaitForSeconds(2);
//         isPreparingNextSegment = false;
//     }

//     void PrepareCompleted(VideoPlayer vp)
//     {
//         Debug.Log("Video prepared. Playing...");
//         vp.Play();
//     }

//     void HandleVideoError(VideoPlayer vp, string message)
//     {
//         Debug.LogError("Error playing video: " + message);
//         metricsLogger.Log($"Error: {message}");
//     }
// }


//working but with switch delay
// using System.Collections;
// using UnityEngine;
// using UnityEngine.Video;

// public class DashVideoPlayer : MonoBehaviour
// {
//     public string mpdURL = "http://192.168.1.201:3000/playable_split/videos/Manifest.mpd"; // URL to the MPD file
//     private VideoPlayer videoPlayer;
//     private MPDParser mpdParser;
//     private SegmentFetcher segmentFetcher;
//     private BufferManager bufferManager;
//     private ABRAlgorithm abrAlgorithm;
//     private MetricsLogger metricsLogger;
//     private bool isPreparingNextSegment = false;
//     public int segmentNumber = 1;

//     void Start()
//     {
//         videoPlayer = GetComponent<VideoPlayer>();
//         mpdParser = new MPDParser();
//         segmentFetcher = gameObject.AddComponent<SegmentFetcher>();
//         bufferManager = BufferManager.Instance;
//         metricsLogger = gameObject.AddComponent<MetricsLogger>();

//         abrAlgorithm = new ABRAlgorithm();

//         bufferManager.InitializeBufferQueue();
//         StartCoroutine(SetupVideoPlayer());
//     }

//     IEnumerator SetupVideoPlayer()
//     {
//         yield return StartCoroutine(mpdParser.FetchMPD(mpdURL));
//         var representations = mpdParser.GetRepresentations();
//         StartCoroutine(segmentFetcher.FetchSegments(representations, abrAlgorithm));
//         StartCoroutine(PlayVideoSegments());
//     }

//     IEnumerator PlayVideoSegments()
//     {
//         while (true)
//         {
//             string filePath = bufferManager.GetNextSegment();
//             metricsLogger.LogBufferStatus(segmentNumber);
//             Debug.Log("Trying to play segment: " + filePath);

//             if (!string.IsNullOrEmpty(filePath))
//             {
//                 videoPlayer.url = "file://" + filePath;
//                 videoPlayer.errorReceived += HandleVideoError;
//                 videoPlayer.prepareCompleted += PrepareCompleted;
//                 videoPlayer.Prepare();

//                 // Wait until the video is prepared before playing it
//                 while (!videoPlayer.isPrepared)
//                 {
//                     yield return null;
//                 }
//                 videoPlayer.Play();
//                 segmentNumber++;
//                 metricsLogger.Log("Video started playing");

//                 // While playing the current segment, prepare the next segment
//                 if (!isPreparingNextSegment)
//                 {
//                     isPreparingNextSegment = true;
//                     StartCoroutine(PrepareNextSegment());
//                 }

//                 // Wait until the video finishes playing
//                 while (videoPlayer.isPlaying)
//                 {
//                     yield return null;
//                 }

//                 metricsLogger.Log("Video finished playing");
//             }
//             else
//             {
//                 yield return null;
//             }
//         }
//     }

//     IEnumerator PrepareNextSegment()
//     {
//         // Fetch the next segment every 2 seconds to keep the buffer ready
//         yield return new WaitForSeconds(2);
//         isPreparingNextSegment = false;
//     }

//     void PrepareCompleted(VideoPlayer vp)
//     {
//         Debug.Log("Video prepared. Playing...");
//         vp.Play();
//     }

//     void HandleVideoError(VideoPlayer vp, string message)
//     {
//         Debug.LogError("Error playing video: " + message);
//         metricsLogger.Log($"Error: {message}");
//     }
// }

// using System.Collections;
// using UnityEngine;
// using UnityEngine.Video;

// public class DashVideoPlayer : MonoBehaviour
// {
//     public string mpdURL = "http://192.168.1.201:3000/playable_split/videos/Manifest.mpd"; // URL to the MPD file
//     private VideoPlayer videoPlayer1;
//     private VideoPlayer videoPlayer2;
//     private VideoPlayer currentPlayer;
//     private VideoPlayer nextPlayer;
//     private MPDParser mpdParser;
//     private SegmentFetcher segmentFetcher;
//     private BufferManager bufferManager;
//     private ABRAlgorithm abrAlgorithm;
//     private MetricsLogger metricsLogger;
//     private bool isNextSegmentReady = false;

//     void Start()
//     {
//         videoPlayer1 = gameObject.AddComponent<VideoPlayer>();
//         videoPlayer2 = gameObject.AddComponent<VideoPlayer>();

//         videoPlayer1.playOnAwake = false;
//         videoPlayer2.playOnAwake = false;

//         currentPlayer = videoPlayer1;
//         nextPlayer = videoPlayer2;

//         mpdParser = new MPDParser();
//         segmentFetcher = gameObject.AddComponent<SegmentFetcher>();
//         bufferManager = BufferManager.Instance;
//         metricsLogger = gameObject.AddComponent<MetricsLogger>();

//         abrAlgorithm = new ABRAlgorithm();

//         bufferManager.InitializeBufferQueue();
//         StartCoroutine(SetupVideoPlayer());
//     }

//     IEnumerator SetupVideoPlayer()
//     {
//         yield return StartCoroutine(mpdParser.FetchMPD(mpdURL));
//         var representations = mpdParser.GetRepresentations();
//         StartCoroutine(segmentFetcher.FetchSegments(representations, abrAlgorithm));
//         StartCoroutine(PlayVideoSegments());
//     }

//     IEnumerator PlayVideoSegments()
//     {
//         while (true)
//         {
//             string filePath = bufferManager.GetNextSegment();
//             metricsLogger.LogBufferStatus(bufferManager.GetBufferQueue().Count);
//             Debug.Log("trying Playing segment: " + filePath);

//             if (!string.IsNullOrEmpty(filePath))
//             {
//                 currentPlayer.url = "file://" + filePath;
//                 currentPlayer.errorReceived += HandleVideoError;
//                 currentPlayer.prepareCompleted += PrepareCompleted;
//                 currentPlayer.Prepare();

//                 // Wait until the video is prepared before playing it
//                 while (!currentPlayer.isPrepared)
//                 {
//                     yield return null;
//                 }

//                 currentPlayer.Play();
//                 metricsLogger.Log("Video started playing");

//                 // Prepare the next segment while the current one is playing
//                 if (!isNextSegmentReady)
//                 {
//                     isNextSegmentReady = true;
//                     StartCoroutine(PrepareNextSegment());
//                 }

//                 // Wait until the video finishes playing
//                 while (currentPlayer.isPlaying)
//                 {
//                     yield return null;
//                 }

//                 metricsLogger.Log("Video finished playing");

//                 // Swap the video players
//                 VideoPlayer temp = currentPlayer;
//                 currentPlayer = nextPlayer;
//                 nextPlayer = temp;
//             }
//             else
//             {
//                 yield return null;
//             }
//         }
//     }

//     IEnumerator PrepareNextSegment()
//     {
//         string nextSegmentPath = bufferManager.GetNextSegment();

//         if (!string.IsNullOrEmpty(nextSegmentPath))
//         {
//             nextPlayer.url = "file://" + nextSegmentPath;
//             nextPlayer.errorReceived += HandleVideoError;
//             nextPlayer.Prepare();

//             // Wait until the next video is prepared
//             while (!nextPlayer.isPrepared)
//             {
//                 yield return null;
//             }

//             metricsLogger.Log("Next video segment prepared");
//             isNextSegmentReady = false;
//         }
//         else
//         {
//             yield return null;
//         }
//     }

//     void PrepareCompleted(VideoPlayer vp)
//     {
//         // This will be called when the current player finishes preparing, start playing the video
//         // We handle playback start in the PlayVideoSegments coroutine, so no action needed here
//     }

//     void HandleVideoError(VideoPlayer vp, string message)
//     {
//         Debug.LogError("Error playing video: " + message);
//         metricsLogger.Log($"Error: {message}");
//     }
// }
