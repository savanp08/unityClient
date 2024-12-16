using System;
using UnityEngine;

public class RateBasedABRController : MonoBehaviour
{
    private readonly int[] bitrates = { 300, 750, 1200, 1850, 2850, 4300 };
    
    private const float RateSafetyMargin = 0.85f; 
    private float lastThroughput = 0f;

    public int RateBasedABRDecision(float currentThroughput)
    {
        lastThroughput = currentThroughput;
        int selectedBitrateIndex = 0;

        for (int i = 0; i < bitrates.Length; i++)
        {
            if (bitrates[i] <= currentThroughput * RateSafetyMargin)
            {
                selectedBitrateIndex = i;
            }
            else
            {
                break;
            }
        }

        Debug.Log("Rate-Based ABR selected bitrate: " + bitrates[selectedBitrateIndex]);
        return bitrates[selectedBitrateIndex];
    }

    void Update()
    {
        float currentThroughput = 2000f; 

        int rateBasedBitrate = RateBasedABRDecision(currentThroughput);
    }
}