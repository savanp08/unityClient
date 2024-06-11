using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using Wave.Essence;
using Wave.Essence.Eye;
using static Wave.Essence.Eye.EyeManager;

public class EyeQoEMetricsLogger : MonoBehaviour
{
    private EyeManager eyeManager;
    private Stopwatch stopwatch;
    private string logFilePath;
    private int fileIndex = 1;
    private ulong sequenceNumber = 0;

    void Awake()
    {
        eyeManager = GameObject.Find("EyeManager").GetComponent<EyeManager>();
        stopwatch = Stopwatch.StartNew();
        logFilePath = Path.Combine(Application.persistentDataPath, $"eye_metrics_log_{fileIndex}.txt");
        while (File.Exists(logFilePath))
        {
            fileIndex++;
            logFilePath = Path.Combine(Application.persistentDataPath, $"eye_metrics_log_{fileIndex}.txt");
        }
    }

    public void CollectAndLogEyeData()
    {
        Dictionary<string, object> eyeData = new Dictionary<string, object>
        {
            { "TimeStamp", stopwatch.Elapsed.TotalMilliseconds },
            { "SequenceNumber", sequenceNumber },
            { "DataType", "Eye" }
        };

        bool isEyeTrackingAvailable = eyeManager.IsEyeTrackingAvailable();
        bool hasEyeTrackingData = eyeManager.HasEyeTrackingData();
        eyeData["IsEyeTrackingAvailable"] = isEyeTrackingAvailable;
        eyeData["HasEyeTrackingData"] = hasEyeTrackingData;

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

        string jsonData = JsonConvert.SerializeObject(eyeData, Formatting.Indented);
        Log(jsonData);

        sequenceNumber++;
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
