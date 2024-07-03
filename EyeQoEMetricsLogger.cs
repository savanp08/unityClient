using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Wave.Essence;
using Wave.Essence.Eye;
using static Wave.Essence.Eye.EyeManager;
using Debug = UnityEngine.Debug;

public class EyeQoEMetricsLogger : MonoBehaviour
{
    private EyeManager eyeManager;
    private Stopwatch stopwatch;
    private string logFilePath;
    private int fileIndex = 1;
    private ulong sequenceNumber = 0;
    private ulong segmentNumber = 0;
    private bool isLogging = false;

    void Awake()
    {
        GameObject eyeManagerObject = GameObject.Find("EyeManager");
        if (eyeManagerObject != null)
        {
            Debug.Log("--->>>  EyeManager component found.");
            eyeManager = eyeManagerObject.GetComponent<EyeManager>();
            if (eyeManager != null)
            {
                Debug.Log("--->>>  EyeManager component enabled.");
                stopwatch = Stopwatch.StartNew();
                logFilePath = Path.Combine(Application.persistentDataPath, $"eye_metrics_log_{fileIndex}.txt");
                while (File.Exists(logFilePath))
                {
                    fileIndex++;
                    logFilePath = Path.Combine(Application.persistentDataPath, $"eye_metrics_log_{fileIndex}.txt");
                }
            }
            else
            {
                Debug.LogError("EyeManager component not found on EyeManager GameObject.");
            }
        }
        else
        {
            Debug.LogError("EyeManager GameObject not found in the scene.");
        }
        
        
    }

    public void StartLoggingEverySecond()
    {
        if (!isLogging)
        {
            isLogging = true;
            InvokeRepeating(nameof(CollectAndLogEyeData), 0f, 1f);
        }
    }

    public void StopLogging()
    {
        if (isLogging)
        {
            isLogging = false;
            CancelInvoke(nameof(CollectAndLogEyeData));
        }
    }

    public void IncreaseSegmentNumber()
    {
        ++segmentNumber;
    }

    public void CollectAndLogEyeData()
    {
        Dictionary<string, object> eyeData = new Dictionary<string, object>
        {
            { "TimeStamp", stopwatch.Elapsed.TotalMilliseconds },
            { "SequenceNumber", sequenceNumber },
            { "SegmentNumber", segmentNumber },
            { "DataType", "Eye" }
        };

        if (eyeManager != null)
        {
            bool isEyeTrackingAvailable = eyeManager.IsEyeTrackingAvailable();
            bool hasEyeTrackingData = eyeManager.HasEyeTrackingData();
            EyeTrackingStatus status = eyeManager.GetEyeTrackingStatus();

            eyeData["IsEyeTrackingAvailable"] = isEyeTrackingAvailable;
            eyeData["HasEyeTrackingData"] = hasEyeTrackingData;
            eyeData["EyeTrackingStatus"] = status.ToString();

            if (!isEyeTrackingAvailable || status == EyeTrackingStatus.NOT_START)
            {
                Debug.Log("--->>> Eye Tracking is not available or not started. Attempting to start...");
                
            }

            if (eyeManager.GetEyeOrigin(EyeType.Combined, out Vector3 combinedEyeOrigin))
            {
                eyeData["CombinedEyeOrigin"] = new SerializableVector3(combinedEyeOrigin);
            }
            if (eyeManager.GetEyeDirectionNormalized(EyeType.Combined, out Vector3 combinedEyeDirection))
            {
                eyeData["CombinedEyeDirection"] = new SerializableVector3(combinedEyeDirection);
            }
            if (eyeManager.GetEyeOrigin(EyeType.Left, out Vector3 leftEyeOrigin))
            {
                eyeData["LeftEyeOrigin"] = new SerializableVector3(leftEyeOrigin);
            }
            if (eyeManager.GetEyeDirectionNormalized(EyeType.Left, out Vector3 leftEyeDirection))
            {
                eyeData["LeftEyeDirection"] = new SerializableVector3(leftEyeDirection);
            }
            if (eyeManager.GetLeftEyeOpenness(out float leftEyeOpenness))
            {
                eyeData["LeftEyeOpenness"] = leftEyeOpenness;
            }
            if (eyeManager.GetLeftEyePupilDiameter(out float leftEyePupilDiameter))
            {
                eyeData["LeftEyePupilDiameter"] = leftEyePupilDiameter;
            }
            if (eyeManager.GetLeftEyePupilPositionInSensorArea(out Vector2 leftEyePupilPositionInSensorArea))
            {
                eyeData["LeftEyePupilPositionInSensorArea"] = new SerializableVector2(leftEyePupilPositionInSensorArea);
            }
            if (eyeManager.GetEyeOrigin(EyeType.Right, out Vector3 rightEyeOrigin))
            {
                eyeData["RightEyeOrigin"] = new SerializableVector3(rightEyeOrigin);
            }
            if (eyeManager.GetEyeDirectionNormalized(EyeType.Right, out Vector3 rightEyeDirection))
            {
                eyeData["RightEyeDirection"] = new SerializableVector3(rightEyeDirection);
            }
            if (eyeManager.GetRightEyeOpenness(out float rightEyeOpenness))
            {
                eyeData["RightEyeOpenness"] = rightEyeOpenness;
            }
            if (eyeManager.GetRightEyePupilDiameter(out float rightEyePupilDiameter))
            {
                eyeData["RightEyePupilDiameter"] = rightEyePupilDiameter;
            }
            if (eyeManager.GetRightEyePupilPositionInSensorArea(out Vector2 rightEyePupilPositionInSensorArea))
            {
                eyeData["RightEyePupilPositionInSensorArea"] = new SerializableVector2(rightEyePupilPositionInSensorArea);
            }

            // Additional data for reinforcement learning analysis
            eyeData["HeadPosePosition"] = new SerializableVector3(Camera.main.transform.position);
            eyeData["HeadPoseRotation"] = new SerializableVector3(Camera.main.transform.eulerAngles);

            string jsonData = JsonConvert.SerializeObject(eyeData, Formatting.Indented);
            Log(jsonData);

            sequenceNumber++;
        }
        else
        {
            Debug.LogError("EyeManager instance is not available.");
        }
    }

    private void Log(string message)
    {
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            writer.WriteLine($"{System.DateTime.Now}: {message}");
        }
    }

    public class SerializableVector3
    {
        public float x;
        public float y;
        public float z;

        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }
    }

    public class SerializableVector2
    {
        public float x;
        public float y;

        public SerializableVector2(Vector2 vector)
        {
            x = vector.x;
            y = vector.y;
        }
    }

    public void TestEyeTracking()
    {
        if (eyeManager != null)
        {
            eyeManager.EnableEyeTracking = true;
            Debug.Log("->>>>>>> Test Eye track : Enabling eye tracking.");
            
            bool isEyeTrackingAvailable = eyeManager.IsEyeTrackingAvailable();
            bool hasEyeTrackingData = eyeManager.HasEyeTrackingData();
            EyeTrackingStatus status2 = eyeManager.GetEyeTrackingStatus();
            Debug.Log("->>>>>>> Test Eye track : Eye tracking status: " + status2);
        

            if (eyeManager.IsEyeTrackingAvailable())
            {
                Debug.Log("->>>>>>> Test Eye track : Eye tracking is available.");
                if (eyeManager.HasEyeTrackingData())
                {
                    Debug.Log("->>>>>>> Test Eye track : Eye data is available.");
                }
                else
                {
                    Debug.Log("->>>>>>> Test Eye track : Eye data is not available.");
                }
            }
            else
            {
                Debug.Log("->>>>>>> Test Eye track : Eye tracking is not available.");
                
                
            }

            EyeTrackingStatus status = eyeManager.GetEyeTrackingStatus();
            Debug.Log("->>>>>>> Test Eye track : Eye tracking status: " + status);
        }
        else
        {
            Debug.LogError("EyeManager instance is not available in TestEyeTracking.");
        }
    }
}


// using Newtonsoft.Json;
// using System.Collections.Generic;
// using System.Diagnostics;
// using System.IO;
// using UnityEngine;
// using Wave.Essence;
// using Wave.Essence.Eye;
// using static Wave.Essence.Eye.EyeManager;
// using Debug = UnityEngine.Debug;

// public class EyeQoEMetricsLogger : MonoBehaviour
// {
//     private EyeManager eyeManager;
//     private Stopwatch stopwatch;
//     private string logFilePath;
//     private int fileIndex = 1;
//     private ulong sequenceNumber = 0;
//     private ulong segmentNumber = 0;
//     private bool isLogging = false;

//     void Awake()
//     {
//         GameObject eyeManagerObject = GameObject.Find("EyeManager");
//         if (eyeManagerObject != null)
//         {
//             Debug.Log("--->>>  EyeManager component found.");
//             eyeManager = eyeManagerObject.GetComponent<EyeManager>();
//             if (eyeManager != null)
//             {
//                 Debug.Log("--->>>  EyeManager component enabled.");
//                 stopwatch = Stopwatch.StartNew();
//                 logFilePath = Path.Combine(Application.persistentDataPath, $"eye_metrics_log_{fileIndex}.txt");
//                 while (File.Exists(logFilePath))
//                 {
//                     fileIndex++;
//                     logFilePath = Path.Combine(Application.persistentDataPath, $"eye_metrics_log_{fileIndex}.txt");
//                 }
//             }
//             else
//             {
//                 Debug.LogError("EyeManager component not found on EyeManager GameObject.");
//             }
//         }
//         else
//         {
//             Debug.LogError("EyeManager GameObject not found in the scene.");
//         }
//         InvokeRepeating(nameof(TestEyeTracking), 0f, 0.2f);
//         StartLoggingEverySecond();
//     }

//     public void StartLoggingEverySecond()
//     {
//         if (!isLogging)
//         {
//             isLogging = true;
//             InvokeRepeating(nameof(CollectAndLogEyeData), 0f, 1f);
//         }
//     }

//     public void StopLogging()
//     {
//         if (isLogging)
//         {
//             isLogging = false;
//             CancelInvoke(nameof(CollectAndLogEyeData));
//         }
//     }

//     public void IncreaseSegmentNumber()
//     {
//         ++segmentNumber;
//     }

//     public void CollectAndLogEyeData()
//     {
//         Dictionary<string, object> eyeData = new Dictionary<string, object>
//         {
//             { "TimeStamp", stopwatch.Elapsed.TotalMilliseconds },
//             { "SequenceNumber", sequenceNumber },
//             { "SegmentNumber", segmentNumber },
//             { "DataType", "Eye" }
//         };

//         bool isEyeTrackingAvailable = eyeManager.IsEyeTrackingAvailable();
//         bool hasEyeTrackingData = eyeManager.HasEyeTrackingData();
//         if (EyeManager.Instance != null) { EyeManager.Instance.EnableEyeTracking = true; }

//         eyeData["IsEyeTrackingAvailable"] = isEyeTrackingAvailable;
//         eyeData["HasEyeTrackingData"] = hasEyeTrackingData;

//         if (eyeManager.GetEyeOrigin(EyeType.Combined, out Vector3 combinedEyeOrigin))
//         {
//             eyeData["CombinedEyeOrigin"] = new SerializableVector3(combinedEyeOrigin);
//         }
//         if (eyeManager.GetEyeDirectionNormalized(EyeType.Combined, out Vector3 combinedEyeDirection))
//         {
//             eyeData["CombinedEyeDirection"] = new SerializableVector3(combinedEyeDirection);
//         }
//         if (eyeManager.GetEyeOrigin(EyeType.Left, out Vector3 leftEyeOrigin))
//         {
//             eyeData["LeftEyeOrigin"] = new SerializableVector3(leftEyeOrigin);
//         }
//         if (eyeManager.GetEyeDirectionNormalized(EyeType.Left, out Vector3 leftEyeDirection))
//         {
//             eyeData["LeftEyeDirection"] = new SerializableVector3(leftEyeDirection);
//         }
//         if (eyeManager.GetLeftEyeOpenness(out float leftEyeOpenness))
//         {
//             eyeData["LeftEyeOpenness"] = leftEyeOpenness;
//         }
//         if (eyeManager.GetLeftEyePupilDiameter(out float leftEyePupilDiameter))
//         {
//             eyeData["LeftEyePupilDiameter"] = leftEyePupilDiameter;
//         }
//         if (eyeManager.GetLeftEyePupilPositionInSensorArea(out Vector2 leftEyePupilPositionInSensorArea))
//         {
//             eyeData["LeftEyePupilPositionInSensorArea"] = new SerializableVector2(leftEyePupilPositionInSensorArea);
//         }
//         if (eyeManager.GetEyeOrigin(EyeType.Right, out Vector3 rightEyeOrigin))
//         {
//             eyeData["RightEyeOrigin"] = new SerializableVector3(rightEyeOrigin);
//         }
//         if (eyeManager.GetEyeDirectionNormalized(EyeType.Right, out Vector3 rightEyeDirection))
//         {
//             eyeData["RightEyeDirection"] = new SerializableVector3(rightEyeDirection);
//         }
//         if (eyeManager.GetRightEyeOpenness(out float rightEyeOpenness))
//         {
//             eyeData["RightEyeOpenness"] = rightEyeOpenness;
//         }
//         if (eyeManager.GetRightEyePupilDiameter(out float rightEyePupilDiameter))
//         {
//             eyeData["RightEyePupilDiameter"] = rightEyePupilDiameter;
//         }
//         if (eyeManager.GetRightEyePupilPositionInSensorArea(out Vector2 rightEyePupilPositionInSensorArea))
//         {
//             eyeData["RightEyePupilPositionInSensorArea"] = new SerializableVector2(rightEyePupilPositionInSensorArea);
//         }

//         // Additional data for reinforcement learning analysis
//         eyeData["HeadPosePosition"] = new SerializableVector3(Camera.main.transform.position);
//         eyeData["HeadPoseRotation"] = new SerializableVector3(Camera.main.transform.eulerAngles);

//         string jsonData = JsonConvert.SerializeObject(eyeData, Formatting.Indented);
//         Log(jsonData);

//         sequenceNumber++;
//     }

//     private void Log(string message)
//     {
//         using (StreamWriter writer = new StreamWriter(logFilePath, true))
//         {
//             writer.WriteLine($"{System.DateTime.Now}: {message}");
//         }
//     }

//     public class SerializableVector3
//     {
//         public float x;
//         public float y;
//         public float z;

//         public SerializableVector3(Vector3 vector)
//         {
//             x = vector.x;
//             y = vector.y;
//             z = vector.z;
//         }
//     }

//     public class SerializableVector2
//     {
//         public float x;
//         public float y;

//         public SerializableVector2(Vector2 vector)
//         {
//             x = vector.x;
//             y = vector.y;
//         }
//     }

//     public void TestEyeTracking()
//     {
//         if(EyeManager.Instance != null){
//             EyeManager.Instance.EnableEyeTracking = true;
//         }
//         if (EyeManager.Instance.IsEyeTrackingAvailable())
//         {
//             Debug.Log("->>>>>>> Test Eye track : Eye tracking is available.");
//             EyeManager.Instance.EnableEyeTracking = true;
//             if (EyeManager.Instance.HasEyeTrackingData())
//             {
//                 Debug.Log("->>>>>>> Test Eye track : Eye data is available.");
//             }
//             else
//             {
//                 Debug.Log("->>>>>>> Test Eye track : Eye data is not available.");
//             }
//         }
//         else
//         {
//             Debug.Log("->>>>>>> Test Eye track : Eye tracking is not available.");
//         }
//     }
// }
