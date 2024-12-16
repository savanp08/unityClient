// using Unity.WebRTC;
// using UnityEngine;
// using System.IO;
// using System.Collections.Generic;

// public class NetworkMetricsLogger : MonoBehaviour
// {
//     private RTCPeerConnection peerConnection;
//     private string filePath = "networkMetrics.csv";
    
//     void Start()
//     {
//         peerConnection = new RTCPeerConnection();      
//         if (!File.Exists(filePath))
//         {
//             using (StreamWriter writer = new StreamWriter(filePath, false))
//             {
//                 writer.WriteLine("Timestamp,Packets Sent,Packets Lost,Jitter,Bytes Sent,Round Trip Time (RTT),Bitrate");
//             }
//         }

      
//         InvokeRepeating(nameof(LogNetworkMetrics), 1f, 4f); 
//     }

//     private void LogNetworkMetrics()
//     {
//         peerConnection.GetStats(OnStatsReady);
//     }

//     private void OnStatsReady(RTCStatsReport statsReport)
//     {
//         Dictionary<string, string> logData = new Dictionary<string, string>();

//         foreach (var stat in statsReport.Stats.Values)
//         {
//             if (stat.Type == RTCStatsType.OutboundRtp) 
//             {
//                 string packetsSent = stat.Stats.ContainsKey("packetsSent") ? stat.Stats["packetsSent"].ToString() : "N/A";
//                 string packetsLost = stat.Stats.ContainsKey("packetsLost") ? stat.Stats["packetsLost"].ToString() : "N/A";
//                 string jitter = stat.Stats.ContainsKey("jitter") ? stat.Stats["jitter"].ToString() : "N/A";
//                 string bytesSent = stat.Stats.ContainsKey("bytesSent") ? stat.Stats["bytesSent"].ToString() : "N/A";

//                 logData["PacketsSent"] = packetsSent;
//                 logData["PacketsLost"] = packetsLost;
//                 logData["Jitter"] = jitter;
//                 logData["BytesSent"] = bytesSent;
//             }
//             else if (stat.Type == RTCStatsType.CandidatePair) // Candidate pair stats
//             {
//                 string rtt = stat.Stats.ContainsKey("currentRoundTripTime") ? stat.Stats["currentRoundTripTime"].ToString() : "N/A";
//                 string bitrate = stat.Stats.ContainsKey("availableOutgoingBitrate") ? stat.Stats["availableOutgoingBitrate"].ToString() : "N/A";

//                 logData["RTT"] = rtt;
//                 logData["Bitrate"] = bitrate;
//             }
//         }

//         // Log the collected data to a file
//         WriteToCsv(logData);
//     }

//     private void WriteToCsv(Dictionary<string, string> logData)
//     {
//         using (StreamWriter writer = new StreamWriter(filePath, true))
//         {
//             string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
//             writer.WriteLine($"{timestamp},{logData.GetValueOrDefault("PacketsSent","N/A")},{logData.GetValueOrDefault("PacketsLost","N/A")},{logData.GetValueOrDefault("Jitter","N/A")},{logData.GetValueOrDefault("BytesSent","N/A")},{logData.GetValueOrDefault("RTT","N/A")},{logData.GetValueOrDefault("Bitrate","N/A")}");
//         }
//     }

//     private void OnDestroy()
//     {
//         peerConnection.Close();
//     }
// }
