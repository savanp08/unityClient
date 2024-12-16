using System.Collections.Generic;
using UnityEngine;                 

public class EyeDataHandler : MonoBehaviour
{
    private Dictionary<int, float[]> eyeDataPerSegment;  // Stores eye data for each segment
    private Dictionary<int, bool> dataReady;

    void Awake()
    {
        eyeDataPerSegment = new Dictionary<int, float[]>();
        dataReady = new Dictionary<int, bool>();
    }

    // Adds eye-tracking data for a specific segment
    public void AddEyeData(int segmentNumber, float[] eyeData)
    {
        eyeDataPerSegment[segmentNumber] = eyeData;
        dataReady[segmentNumber] = true;  // Mark data as ready
    }

    // Checks if eye-tracking data is ready for a segment
    public bool IsDataReady(int segmentNumber)
    {
        return dataReady.ContainsKey(segmentNumber) && dataReady[segmentNumber];
    }

    // Retrieves eye-tracking data for a segment
    public float[] GetEyeData(int segmentNumber)
    {
        if (IsDataReady(segmentNumber))
        {
            dataReady[segmentNumber] = false;  // Mark data as used
            return eyeDataPerSegment[segmentNumber];
        }
        return null;  // Return null if data is not ready
    }
}
