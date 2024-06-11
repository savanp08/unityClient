using System.IO;
using UnityEngine;

public class MetricsLogger : MonoBehaviour
{
    private string logFilePath;

    void Awake()
    {
        logFilePath = GenerateLogFilePath();
    }

    private string GenerateLogFilePath()
    {
        string directory = Application.persistentDataPath;
        string baseFileName = "metrics_log";
        string extension = ".txt";
        int fileNumber = 1;

        string filePath = Path.Combine(directory, $"{baseFileName}{fileNumber}{extension}");

        while (File.Exists(filePath))
        {
            fileNumber++;
            filePath = Path.Combine(directory, $"{baseFileName}{fileNumber}{extension}");
        }

        // Create the new file
        File.Create(filePath).Dispose();

        return filePath;
    }

    public void Log(string message)
    {
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            writer.WriteLine($"{System.DateTime.Now}: {message}");
        }
    }

    public void LogBandwidth(long bandwidth)
    {
        Log($"Bandwidth: {bandwidth} bps");
    }

    public void LogNetworkSpeed(long speed)
    {
        Log($"Network Speed: {speed} bps");
    }

    public void LogBitrate(int bitrate)
    {
        Log($"Bitrate: {bitrate} bps");
    }

    public void LogDelay(float delay)
    {
        Log($"Delay: {delay} seconds");
    }

    public void LogLatency(float latency)
    {
        Log($"Latency: {latency} seconds");
    }

    public void LogBufferStatus(int bufferLength)
    {
        Log($"Buffer Length: {bufferLength} segments");
    }
    public void LogRepresentation(string representationID)
    {
        Log($"Representation: {representationID}");
    }
    public void LogSegmentNumber(int segmentNumber)
    {
        Log($"Segment Number: {segmentNumber}");
    }

}


/*
thourough put
 (current, past)


*/