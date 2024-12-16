using System;
using UnityEngine;

public class MPEGDASHABRController : MonoBehaviour
{
    private readonly int[] bitrates = { 300, 750, 1200, 1850, 2850, 4300 };
    
    private float currentThroughput = 0f;
    private float bufferLevel = 0f;
    private const float SafetyMargin = 0.9f; 
    private const float MinBufferLevel = 5f;  

    // Function to call for making an ABR decision using MPEG-DASH logic
    public int MPEGDASHABRDecision(float currentThroughput, float bufferLevel)
    {
        this.currentThroughput = currentThroughput;
        this.bufferLevel = bufferLevel;
        int selectedBitrateIndex = 0;

        // Check the buffer level to determine if we need to switch down
        if (bufferLevel < MinBufferLevel)
        {
            // Buffer is too low, switch to the lowest bitrate to avoid rebuffering
            selectedBitrateIndex = 0;
            Debug.Log("Buffer level is low, switching to lowest bitrate.");
        }
        else
        {
            for (int i = 0; i < bitrates.Length; i++)
            {
                if (bitrates[i] <= currentThroughput * SafetyMargin)
                {
                    selectedBitrateIndex = i;
                }
                else
                {
                    break;
                }
            }
        }

        Debug.Log("MPEG-DASH ABR selected bitrate: " + bitrates[selectedBitrateIndex]);
        return bitrates[selectedBitrateIndex];
    }

 
    void Update()
    {
        
        // Select bitrate based on MPEG-DASH ABR
        int selectedBitrate = MPEGDASHABRDecision(simulatedThroughput, simulatedBufferLevel);
    }
}

