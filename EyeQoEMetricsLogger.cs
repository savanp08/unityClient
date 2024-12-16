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
    public EyeManager eyeManager;
    public Stopwatch stopwatch;
    public string logFilePath;
    public string csvFilePath;
    public string mlLogFilePath = "";
    public StreamWriter csvWriter;
    public int fileIndex = 1;
    public ulong sequenceNumber = 0;
    public ulong segmentNumber = 0;
    public bool isLogging = false;
    public DashVideoMaster dashVideoMaster;

    public const int MaxSegments = 15;
    public List<SegmentData> segmentEyeDataBuffer;
    public SegmentStatus[] segmentStatuses;
    
    public enum SegmentStatus
    {
        Recording,
        ReadyForModel,
        CollectedByModel
    }

    public class SegmentData
    {
        public ulong SegmentNumber;
        public List<Dictionary<string, object>> EyeDataEntries;
        public ulong TotalEyeDataEntries;  // Track the number of eye data entries logged for this segment

        public SegmentData(ulong segmentNumber)
        {
            SegmentNumber = segmentNumber;
            EyeDataEntries = new List<Dictionary<string, object>>();
            TotalEyeDataEntries = 0; // Initialize metadata
        }
    }

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
                mlLogFilePath = Path.Combine(Application.persistentDataPath, $"ml_interaction_log_{fileIndex}.txt");
                InitializeSegmentBuffers();
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
        csvFilePath = Path.Combine(Application.persistentDataPath, $"EyeTrackingData_{fileIndex}_video{dashVideoMaster.currentVideoID}_quality{dashVideoMaster.currentQualityIndex}.csv");
        csvWriter = new StreamWriter(csvFilePath, true);
        // Write CSV header
        csvWriter.WriteLine("Timestamp,SequenceNumber,SegmentNumber,LeftEyeOriginX,LeftEyeOriginY,LeftEyeOriginZ,RightEyeOriginX,RightEyeOriginY,RightEyeOriginZ,CombinedEyeOriginX,CombinedEyeOriginY,CombinedEyeOriginZ,LeftEyeDirectionX,LeftEyeDirectionY,LeftEyeDirectionZ,RightEyeDirectionX,RightEyeDirectionY,RightEyeDirectionZ,CombinedEyeDirectionX,CombinedEyeDirectionY,CombinedEyeDirectionZ,LeftEyeOpenness,RightEyeOpenness,LeftEyePupilDiameter,RightEyePupilDiameter,LeftEyePupilPositionX,LeftEyePupilPositionY,RightEyePupilPositionX,RightEyePupilPositionY,HeadPosePositionX,HeadPosePositionY,HeadPosePositionZ,HeadPoseRotationX,HeadPoseRotationY,HeadPoseRotationZ");
    }

    void InitializeSegmentBuffers()
    {
        segmentEyeDataBuffer = new List<SegmentData>(MaxSegments);
        segmentStatuses = new SegmentStatus[MaxSegments];
        
        for (int i = 0; i < MaxSegments; i++)
        {
            segmentEyeDataBuffer.Add(new SegmentData((ulong)i));
            segmentStatuses[i] = SegmentStatus.Recording;
        }
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
        int currentSegmentIndex = (int)(segmentNumber % MaxSegments);
        segmentStatuses[currentSegmentIndex] = SegmentStatus.ReadyForModel;

        segmentNumber++;
        segmentEyeDataBuffer[currentSegmentIndex] = new SegmentData(segmentNumber - 1);  // Reset for next segment, keep the old segment number for the newly reset segment
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
                LogMLInteraction("Eye Tracking is not available or not started.");
            }

            Vector3 leftEyeOrigin, rightEyeOrigin, combinedEyeOrigin;
            Vector3 leftEyeDirection, rightEyeDirection, combinedEyeDirection;
            float leftEyeOpenness, rightEyeOpenness;
            float leftEyePupilDiameter, rightEyePupilDiameter;
            Vector2 leftEyePupilPositionInSensorArea, rightEyePupilPositionInSensorArea;

            // Collect eye tracking data from EyeManager
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

            // Fall back to retry getting pupil diameter if it returns zero
            if (leftEyePupilDiameter == 0)
            {
                EyeManager.Instance.GetLeftEyePupilDiameter(out leftEyePupilDiameter);
            }
            if (rightEyePupilDiameter == 0)
            {
                EyeManager.Instance.GetRightEyePupilDiameter(out rightEyePupilDiameter);
            }

            // Additional data for reinforcement learning analysis
            Vector3 headPosePosition = Camera.main.transform.position;
            Vector3 headPoseRotation = Camera.main.transform.eulerAngles;

            // Adding all necessary data to eyeData dictionary
            eyeData["LeftEyeOriginX"] = leftEyeOrigin.x;
            eyeData["LeftEyeOriginY"] = leftEyeOrigin.y;
            eyeData["LeftEyeOriginZ"] = leftEyeOrigin.z;
            
            eyeData["RightEyeOriginX"] = rightEyeOrigin.x;
            eyeData["RightEyeOriginY"] = rightEyeOrigin.y;
            eyeData["RightEyeOriginZ"] = rightEyeOrigin.z;
            
            eyeData["CombinedEyeOriginX"] = combinedEyeOrigin.x;
            eyeData["CombinedEyeOriginY"] = combinedEyeOrigin.y;
            eyeData["CombinedEyeOriginZ"] = combinedEyeOrigin.z;
            
            eyeData["LeftEyeDirectionX"] = leftEyeDirection.x;
            eyeData["LeftEyeDirectionY"] = leftEyeDirection.y;
            eyeData["LeftEyeDirectionZ"] = leftEyeDirection.z;
            
            eyeData["RightEyeDirectionX"] = rightEyeDirection.x;
            eyeData["RightEyeDirectionY"] = rightEyeDirection.y;
            eyeData["RightEyeDirectionZ"] = rightEyeDirection.z;
            
            eyeData["CombinedEyeDirectionX"] = combinedEyeDirection.x;
            eyeData["CombinedEyeDirectionY"] = combinedEyeDirection.y;
            eyeData["CombinedEyeDirectionZ"] = combinedEyeDirection.z;
            
            eyeData["LeftEyeOpenness"] = leftEyeOpenness;
            eyeData["RightEyeOpenness"] = rightEyeOpenness;
            
            eyeData["LeftEyePupilDiameter"] = leftEyePupilDiameter;
            eyeData["RightEyePupilDiameter"] = rightEyePupilDiameter;
            
            eyeData["LeftEyePupilPositionX"] = leftEyePupilPositionInSensorArea.x;
            eyeData["LeftEyePupilPositionY"] = leftEyePupilPositionInSensorArea.y;
            
            eyeData["RightEyePupilPositionX"] = rightEyePupilPositionInSensorArea.x;
            eyeData["RightEyePupilPositionY"] = rightEyePupilPositionInSensorArea.y;
            
            eyeData["HeadPosePositionX"] = headPosePosition.x;
            eyeData["HeadPosePositionY"] = headPosePosition.y;
            eyeData["HeadPosePositionZ"] = headPosePosition.z;
            
            eyeData["HeadPoseRotationX"] = headPoseRotation.x;
            eyeData["HeadPoseRotationY"] = headPoseRotation.y;
            eyeData["HeadPoseRotationZ"] = headPoseRotation.z;

            // Store the eye data in the current segment's buffer
            int currentSegmentIndex = (int)(segmentNumber % MaxSegments);
            if (segmentStatuses[currentSegmentIndex] == SegmentStatus.Recording)
            {
                SegmentData currentSegment = segmentEyeDataBuffer[currentSegmentIndex];
                currentSegment.EyeDataEntries.Add(eyeData);
                currentSegment.TotalEyeDataEntries++;  // Update the number of entries for the current segment
                Debug.Log($"Eye data added to segment {currentSegmentIndex}. Total entries: {currentSegment.TotalEyeDataEntries}");
                LogMLInteraction($"Eye data added to segment {currentSegmentIndex}. Total entries: {currentSegment.TotalEyeDataEntries}");
            }

            // Write to CSV
            string csvLine = $"{stopwatch.Elapsed.TotalMilliseconds},{sequenceNumber},{segmentNumber},{leftEyeOrigin.x},{leftEyeOrigin.y},{leftEyeOrigin.z},{rightEyeOrigin.x},{rightEyeOrigin.y},{rightEyeOrigin.z},{combinedEyeOrigin.x},{combinedEyeOrigin.y},{combinedEyeOrigin.z},{leftEyeDirection.x},{leftEyeDirection.y},{leftEyeDirection.z},{rightEyeDirection.x},{rightEyeDirection.y},{rightEyeDirection.z},{combinedEyeDirection.x},{combinedEyeDirection.y},{combinedEyeDirection.z},{leftEyeOpenness},{rightEyeOpenness},{leftEyePupilDiameter},{rightEyePupilDiameter},{leftEyePupilPositionInSensorArea.x},{leftEyePupilPositionInSensorArea.y},{rightEyePupilPositionInSensorArea.x},{rightEyePupilPositionInSensorArea.y},{headPosePosition.x},{headPosePosition.y},{headPosePosition.z},{headPoseRotation.x},{headPoseRotation.y},{headPoseRotation.z}";
            csvWriter.WriteLine(csvLine);
            csvWriter.Flush();
            Debug.Log("Eye data written to CSV.");
            LogMLInteraction("Eye data written to CSV.");

            // Log data as JSON for potential other use cases
            string jsonData = JsonConvert.SerializeObject(eyeData, Formatting.Indented);
            Log(jsonData);

            sequenceNumber++;
        }
        else
        {
            Debug.LogError("EyeManager instance is not available.");
            LogMLInteraction("EyeManager instance is not available.");
        }
    }

    public SegmentData GetSegmentData(int segmentIndex)
    {
        Debug.Log($"Requested segment data for index {segmentIndex}");
        LogMLInteraction($"Requested segment data for index {segmentIndex}");
        if (segmentIndex >= MaxSegments)
        {
            Debug.LogError("Requested segment index exceeds maximum buffer size.");
            LogMLInteraction("Requested segment index exceeds maximum buffer size.");
            return null;
        }

        if (segmentStatuses[segmentIndex] == SegmentStatus.ReadyForModel)
        {
            SegmentData segmentData = segmentEyeDataBuffer[segmentIndex];

            // Validate that the segment index matches the segment number
            if (segmentData != null && segmentData.SegmentNumber == (ulong)segmentIndex)
            {
                segmentStatuses[segmentIndex] = SegmentStatus.CollectedByModel;
                Debug.Log($"Segment data for index {segmentIndex} is ready and collected.");
                LogMLInteraction($"Segment data for index {segmentIndex} is ready and collected.");
                return segmentData;
            }
            else
            {
                Debug.LogError($"Mismatch in requested segment number for index {segmentIndex}");
                LogMLInteraction($"Mismatch in requested segment number for index {segmentIndex}");
            }
        }
        else
        {
            Debug.LogError("Requested segment data is not ready or already collected.");
            LogMLInteraction("Requested segment data is not ready or already collected.");
        }
        return null;
    }

    public void Log(string message)
    {
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            writer.WriteLine($"{System.DateTime.Now}: {message}");
        }
    }

    public void LogMLInteraction(string message)
    {
        using (StreamWriter writer = new StreamWriter(mlLogFilePath, true))
        {
            writer.WriteLine($"{System.DateTime.Now}: {message}");
        }
    }
}
