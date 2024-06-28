using System.IO;
using UnityEngine;

public class CustomLogger : MonoBehaviour
{
    private string logFilePath;

    void Awake()
    {
    int i = 0;
    string eyeFileDebugName = $"eye_debug{i}.txt";
    while (true)
    {
        if (File.Exists(Path.Combine(Application.persistentDataPath,eyeFileDebugName)))
        {
        i += 1;
        eyeFileDebugName = $"eye_debug{i}.txt";
        }
        else
        {
        break;
        }
    }
        logFilePath = Path.Combine(Application.persistentDataPath, eyeFileDebugName);
        Application.logMessageReceived += LogMessage;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= LogMessage;
    }

    private void LogMessage(string condition, string stackTrace, LogType type)
    {
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            writer.WriteLine($"{System.DateTime.Now}: [{type}] {condition}");
        }
    }
}
