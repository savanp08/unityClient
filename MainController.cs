// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.UI;

// public class MainVideoController : MonoBehaviour
// {
//     // Array of .mpd URLs
//     public List<string> streamUrls;
//     public RawImage bufferScreen;

//     private int currentVideoIndex = 0;
//     private DashVideoPlayer dashVideoPlayer;

//     void Start()
//     {
//         // Get the DashVideoPlayer component attached to the same GameObject
//         dashVideoPlayer = GetComponent<DashVideoPlayer>();
//         if (dashVideoPlayer == null)
//         {
//             Debug.LogError("DashVideoPlayer component not found on the same GameObject.");
//             return;
//         }
//         Debug.Log("->>>> url count : " + streamUrls.Count);
//         if(streamUrls.Count == 0)
//         {
//             Debug.LogError("No stream URLs provided.");
//             streamUrls.Add("http://192.168.1.201:3000/playable_split/videos/Manifest.mpd");
//         }

//         StartCoroutine(PlaySequentially());
//     }

//     private IEnumerator PlaySequentially()
//     {
//         while (currentVideoIndex < streamUrls.Count)
//         {
//             dashVideoPlayer.StartPlayBack(streamUrls[currentVideoIndex], OnVideoFinished, bufferScreen);
//             yield return new WaitUntil(() => dashVideoPlayer.IsVideoFinished);
//             currentVideoIndex++;
//             Debug.Log("->>>> Playing next video from manifest url :" + streamUrls[currentVideoIndex]);
//         }

//         Debug.Log("All videos played.");
//     }

//     private void OnVideoFinished()
//     {
//         // This function can be used to handle any additional actions when a video finishes
//     }
// }
