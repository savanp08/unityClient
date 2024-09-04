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
    private string csvFilePath;
    private StreamWriter csvWriter;
    private int fileIndex = 1;
    private ulong sequenceNumber = 0;
    private ulong segmentNumber = 0;
    private bool isLogging = false;

    void Awake()
    {
        if (EyeManager.Instance != null) { EyeManager.Instance.EnableEyeTracking = true; }
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
                InitializeCSVLogging();
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

    void InitializeCSVLogging()
    {
        csvFilePath = Path.Combine(Application.persistentDataPath, $"EyeTrackingData_{fileIndex}.csv");
        csvWriter = new StreamWriter(csvFilePath, true);
        // Write CSV header
        csvWriter.WriteLine("Timestamp,SequenceNumber,SegmentNumber,LeftEyeOriginX,LeftEyeOriginY,LeftEyeOriginZ,RightEyeOriginX,RightEyeOriginY,RightEyeOriginZ,CombinedEyeOriginX,CombinedEyeOriginY,CombinedEyeOriginZ,LeftEyeDirectionX,LeftEyeDirectionY,LeftEyeDirectionZ,RightEyeDirectionX,RightEyeDirectionY,RightEyeDirectionZ,CombinedEyeDirectionX,CombinedEyeDirectionY,CombinedEyeDirectionZ,LeftEyeOpenness,RightEyeOpenness,LeftEyePupilDiameter,RightEyePupilDiameter,LeftEyePupilPositionX,LeftEyePupilPositionY,RightEyePupilPositionX,RightEyePupilPositionY,HeadPosePositionX,HeadPosePositionY,HeadPosePositionZ,HeadPoseRotationX,HeadPoseRotationY,HeadPoseRotationZ");
    }

    public void StartLoggingEverySecond()
    {
        if (!isLogging)
        {
            isLogging = true;
            InvokeRepeating(nameof(CollectAndLogEyeData), 0f, 0.005f);
        }
    }

    public void StopLogging()
    {
        if (isLogging)
        {
            isLogging = false;
            CancelInvoke(nameof(CollectAndLogEyeData));
            csvWriter.Close();
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

            Vector3 leftEyeOrigin, rightEyeOrigin, combinedEyeOrigin;
            Vector3 leftEyeDirection, rightEyeDirection, combinedEyeDirection;
            float leftEyeOpenness, rightEyeOpenness;
            float leftEyePupilDiameter, rightEyePupilDiameter;
            Vector2 leftEyePupilPositionInSensorArea, rightEyePupilPositionInSensorArea;

            eyeManager.GetEyeOrigin(EyeType.Combined, out combinedEyeOrigin);
            eyeManager.GetEyeDirectionNormalized(EyeType.Combined, out combinedEyeDirection);
            eyeManager.GetEyeOrigin(EyeType.Left, out leftEyeOrigin);
            eyeManager.GetEyeDirectionNormalized(EyeType.Left, out leftEyeDirection);
            eyeManager.GetLeftEyeOpenness(out leftEyeOpenness);
            eyeManager.GetLeftEyePupilDiameter(out leftEyePupilDiameter);
            eyeManager.GetLeftEyePupilPositionInSensorArea(out leftEyePupilPositionInSensorArea);
            eyeManager.GetEyeOrigin(EyeType.Right, out rightEyeOrigin);
            eyeManager.GetEyeDirectionNormalized(EyeType.Right, out rightEyeDirection);
            eyeManager.GetRightEyeOpenness(out rightEyeOpenness);
            eyeManager.GetRightEyePupilDiameter(out rightEyePupilDiameter);
            eyeManager.GetRightEyePupilPositionInSensorArea(out rightEyePupilPositionInSensorArea);
            if(leftEyePupilDiameter == 0){
                EyeManager.Instance.GetLeftEyePupilDiameter(out leftEyePupilDiameter);
            }
            if(rightEyePupilDiameter == 0){
                EyeManager.Instance.GetRightEyePupilDiameter(out rightEyePupilDiameter);
            }

            // Additional data for reinforcement learning analysis
            Vector3 headPosePosition = Camera.main.transform.position;
            Vector3 headPoseRotation = Camera.main.transform.eulerAngles;

            // Create CSV line
            string csvLine = $"{stopwatch.Elapsed.TotalMilliseconds},{sequenceNumber},{segmentNumber},{leftEyeOrigin.x},{leftEyeOrigin.y},{leftEyeOrigin.z},{rightEyeOrigin.x},{rightEyeOrigin.y},{rightEyeOrigin.z},{combinedEyeOrigin.x},{combinedEyeOrigin.y},{combinedEyeOrigin.z},{leftEyeDirection.x},{leftEyeDirection.y},{leftEyeDirection.z},{rightEyeDirection.x},{rightEyeDirection.y},{rightEyeDirection.z},{combinedEyeDirection.x},{combinedEyeDirection.y},{combinedEyeDirection.z},{leftEyeOpenness},{rightEyeOpenness},{leftEyePupilDiameter},{rightEyePupilDiameter},{leftEyePupilPositionInSensorArea.x},{leftEyePupilPositionInSensorArea.y},{rightEyePupilPositionInSensorArea.x},{rightEyePupilPositionInSensorArea.y},{headPosePosition.x},{headPosePosition.y},{headPosePosition.z},{headPoseRotation.x},{headPoseRotation.y},{headPoseRotation.z}";

            csvWriter.WriteLine(csvLine);
            csvWriter.Flush();

            // Log data as JSON for potential other use cases
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
}
