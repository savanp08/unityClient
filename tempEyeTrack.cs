using System.Collections;
using System.IO;
using UnityEngine;
using Wave.Essence.Eye;

public class EyeTrackingDataCollector : MonoBehaviour
{
    private string filePath;
    private StreamWriter writer;
    private EyeQoEMetricsLogger eyeQoEMetricsLogger;

    void Start()
    {
        eyeQoEMetricsLogger = gameObject.AddComponent<EyeQoEMetricsLogger>();
        eyeQoEMetricsLogger.StartLoggingEverySecond();

        if (EyeManager.Instance != null) { EyeManager.Instance.EnableEyeTracking = true; }
        
        // Initialize file path and writer
        filePath = Path.Combine(Application.persistentDataPath, "EyeTrackingData.csv");
        writer = new StreamWriter(filePath, true);

        // Write CSV header
        writer.WriteLine("Timestamp,LeftEyeOriginX,LeftEyeOriginY,LeftEyeOriginZ,RightEyeOriginX,RightEyeOriginY,RightEyeOriginZ,CombinedEyeOriginX,CombinedEyeOriginY,CombinedEyeOriginZ,LeftEyeDirectionX,LeftEyeDirectionY,LeftEyeDirectionZ,RightEyeDirectionX,RightEyeDirectionY,RightEyeDirectionZ,CombinedEyeDirectionX,CombinedEyeDirectionY,CombinedEyeDirectionZ,LeftEyeOpenness,RightEyeOpenness,LeftEyePupilDiameter,RightEyePupilDiameter,LeftEyePupilPositionX,LeftEyePupilPositionY,RightEyePupilPositionX,RightEyePupilPositionY");

        // Enable Eye Tracking
        if (EyeManager.Instance != null)
        {
            Debug.Log("----->>>>>> EyeManager is available.");
            EyeManager.Instance.EnableEyeTracking = true;

            EyeManager.Instance.GetLeftEyeOrigin(out Vector3 leftOrigin);
            EyeManager.Instance.GetRightEyeOrigin(out Vector3 rightOrigin);
            EyeManager.Instance.GetCombinedEyeOrigin(out Vector3 combinedOrigin);
            Debug.Log($"----->>>>>> Left Eye Origin: {leftOrigin}");
            Debug.Log($"----->>>>>> Right Eye Origin: {rightOrigin}");
            Debug.Log($"----->>>>>> Combined Eye Origin: {combinedOrigin}");

            // Check and start eye tracking if not started
            if (EyeManager.Instance.GetEyeTrackingStatus() == EyeManager.EyeTrackingStatus.NOT_START)
            {
                Debug.Log("----->>>>>> Starting Eye Tracking.");
                
            }
        }
        else
        {
            Debug.LogError("----->>>>>> EyeManager is not available.");
        }
    }

    void Update()
    {
        if (EyeManager.Instance != null)
        {
            // Log the eye tracking status
            EyeManager.EyeTrackingStatus status = EyeManager.Instance.GetEyeTrackingStatus();
            Debug.Log("----->>>>>> Eye Tracking Status: " + status);

            // Check if eye tracking data is available
            bool hasData = EyeManager.Instance.HasEyeTrackingData();
            Debug.Log("----->>>>>> Eye Tracking Data Available: " + hasData);

            if (EyeManager.Instance.IsEyeTrackingAvailable())
            {
                Vector3 leftOrigin, rightOrigin, combinedOrigin;
                Vector3 leftDirection, rightDirection, combinedDirection;
                float leftOpenness, rightOpenness;
                float leftPupilDiameter, rightPupilDiameter;
                Vector2 leftPupilPosition, rightPupilPosition;

                EyeManager.Instance.GetLeftEyeOrigin(out leftOrigin);
                EyeManager.Instance.GetRightEyeOrigin(out rightOrigin);
                EyeManager.Instance.GetCombinedEyeOrigin(out combinedOrigin);

                EyeManager.Instance.GetLeftEyeDirectionNormalized(out leftDirection);
                EyeManager.Instance.GetRightEyeDirectionNormalized(out rightDirection);
                EyeManager.Instance.GetCombindedEyeDirectionNormalized(out combinedDirection);

                EyeManager.Instance.GetLeftEyeOpenness(out leftOpenness);
                EyeManager.Instance.GetRightEyeOpenness(out rightOpenness);

                EyeManager.Instance.GetLeftEyePupilDiameter(out leftPupilDiameter);
                EyeManager.Instance.GetRightEyePupilDiameter(out rightPupilDiameter);
                Debug.Log($"----->>>>>> Left Eye Pupil Diameter: {leftPupilDiameter}");

                EyeManager.Instance.GetLeftEyePupilPositionInSensorArea(out leftPupilPosition);
                EyeManager.Instance.GetRightEyePupilPositionInSensorArea(out rightPupilPosition);

                string timestamp = Time.time.ToString();
                string line = $"{timestamp},{leftOrigin.x},{leftOrigin.y},{leftOrigin.z},{rightOrigin.x},{rightOrigin.y},{rightOrigin.z},{combinedOrigin.x},{combinedOrigin.y},{combinedOrigin.z},{leftDirection.x},{leftDirection.y},{leftDirection.z},{rightDirection.x},{rightDirection.y},{rightDirection.z},{combinedDirection.x},{combinedDirection.y},{combinedDirection.z},{leftOpenness},{rightOpenness},{leftPupilDiameter},{rightPupilDiameter},{leftPupilPosition.x},{leftPupilPosition.y},{rightPupilPosition.x},{rightPupilPosition.y}";

                writer.WriteLine(line);
            }
            else
            {
                if (EyeManager.Instance == null)
                {
                    Debug.LogError("----->>>>>> EyeManager is not available.");
                }
                else
                {
                    Debug.LogError("----->>>>>> Eye Tracking is not available.");
                }
            }

            if (EyeManager.Instance != null)
            {
                Debug.Log("----->>>>>>  Test2: EyeManager is available.");
                EyeManager.Instance.EnableEyeTracking = true;
                if (EyeManager.Instance.IsEyeTrackingAvailable())
                {
                    Debug.Log("----->>>>>> Test2: Eye Tracking is available.");
                    EyeManager.Instance.GetLeftEyeOrigin(out Vector3 leftOrigin);
                    EyeManager.Instance.GetRightEyeOrigin(out Vector3 rightOrigin);
                    EyeManager.Instance.GetCombinedEyeOrigin(out Vector3 combinedOrigin);
                    Debug.Log($"----->>>>>> Test2: Left Eye Origin: {leftOrigin}");
                    Debug.Log($"----->>>>>> Test2: Right Eye Origin: {rightOrigin}");
                    Debug.Log($"----->>>>>> Test2: Combined Eye Origin: {combinedOrigin}");
                    if(EyeManager.Instance.GetLeftEyePupilDiameter(out float leftPupilDiameter)){
                        Debug.Log($"----->>>>>> Test2: Left Eye Pupil Diameter: {leftPupilDiameter}");
                    }
                    else{
                        Debug.LogError("----->>>>>> Test2: Failed to get Left Eye Pupil Diameter.");
                    }

                }
                else
                {
                    Debug.LogError("----->>>>>> Test2: Eye Tracking is not available.");
                }
            }

            if (EyeManager.Instance != null)
            {
                try
                {
                    EyeManager.EyeTrackingStatus status2 = EyeManager.Instance.GetEyeTrackingStatus();
                    Debug.Log("----->>>>>> Test2: Eye Tracking Status: " + status2);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("----->>>>>> Test2: Eye Tracking Status attempt error: " + e);
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        // Close the writer
        writer.Close();
    }
}
