
/*
Partial Working solution with baseline videos 
*/


// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;
// using UnityEngine.Networking;
// using UnityEngine.Video;

// public class VideoPlayerController : MonoBehaviour
// {
//     public VideoPlayer videoPlayer1;
//     public VideoPlayer videoPlayer2;
//     public RenderTexture renderTexture1;
//     public RenderTexture renderTexture2;
//     public Material skyboxMaterial;
//     public int segmentNumber = 1;
//     public float switchTimeBeforeEnd = 0.9f; // Time in seconds before the video ends to switch
//     private string videoDirectory;

//     void Start()
//     {
//         videoDirectory = Path.Combine(Application.persistentDataPath, "Videos");

//         videoPlayer1.targetTexture = renderTexture1;
//         videoPlayer2.targetTexture = renderTexture2;

//         // Ensure the skybox material uses the render texture of the currently active video player
//         skyboxMaterial.mainTexture = renderTexture1;

//         StartCoroutine(PlayNextVideo(videoPlayer1, renderTexture1));
//     }

//     private IEnumerator PlayNextVideo(VideoPlayer videoPlayer, RenderTexture renderTexture)
//     {
//         if (segmentNumber > 10) 
//         {
//             Debug.Log("All videos played");
//             yield break;
//         }

//         string filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");
//         Debug.Log($"Playing video: {filePath}");

//         videoPlayer.url = filePath;
//         videoPlayer.prepareCompleted += videoPrepared;
//         videoPlayer.Prepare();
//     }

//     private void videoPrepared(VideoPlayer videoPlayer)
//     {
//         if(segmentNumber > 10) 
//         {
//             Debug.Log("All videos played");
//             return;
//         }
//         Debug.Log("Video prepared");
//         Debug.Log("Playing video : "+ segmentNumber);
//         if(videoPlayer == videoPlayer1) {
//             skyboxMaterial.mainTexture = renderTexture1;
//         }
//         else {
//             skyboxMaterial.mainTexture = renderTexture2;
//         }
//         videoPlayer.Play();
//         videoPlayer.prepareCompleted -= videoPrepared;

//         videoPlayer.loopPointReached += OnVideoEnded;

//         // Schedule the preparation of the next segment a few milliseconds before the current one ends
//         StartCoroutine(PrepareNextSegment(videoPlayer));
//     }

//     private IEnumerator PrepareNextSegment(VideoPlayer currentVideoPlayer)
//     {
//         yield return new WaitForSeconds((float)currentVideoPlayer.length - switchTimeBeforeEnd);

//         segmentNumber++;
//         if (segmentNumber > 10)
//         {
//             Debug.Log("All videos played");
//             yield break;
//         }

//         string nextFilePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");
//         if (currentVideoPlayer == videoPlayer1)
//         {
//             videoPlayer2.url = nextFilePath;
//             videoPlayer2.prepareCompleted += videoPrepared;
//             videoPlayer2.Prepare();
//         }
//         else
//         {
//             videoPlayer1.url = nextFilePath;
//             videoPlayer1.prepareCompleted += videoPrepared;
//             videoPlayer1.Prepare();
//         }
//     }

//     private void OnVideoEnded(VideoPlayer videoPlayer)
//     {
//         Debug.Log("Video ended");
//         videoPlayer.loopPointReached -= OnVideoEnded;
//     }
// }
