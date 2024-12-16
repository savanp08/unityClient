using System;
using UnityEngine;

public class BolaABRController : MonoBehaviour
{
    private readonly int[] bitrates = { 300, 750, 1200, 1850, 2850, 4300 };
    
    private const float BolaUtilitySlope = 0.75f;
    private const float BolaBufferTarget = 20f;
    private float bolaV = 0f;
    private float bufferLevel = 0f;

    void Start()
    {
        bolaV = CalculateBolaV();
    }

    public int BolaABRDecision(float bufferLevel)
    {
        this.bufferLevel = bufferLevel;
        int selectedBitrateIndex = 0;
        float maxUtility = float.MinValue;

        for (int i = 0; i < bitrates.Length; i++)
        {
            float utility = CalculateBolaUtility(bitrates[i], bufferLevel);
            if (utility > maxUtility)
            {
                maxUtility = utility;
                selectedBitrateIndex = i;
            }
        }

        Debug.Log("BOLA ABR selected bitrate: " + bitrates[selectedBitrateIndex]);
        return bitrates[selectedBitrateIndex];
    }

    private float CalculateBolaUtility(int bitrate, float bufferLevel)
    {
        float qualityUtility = Mathf.Log((float)bitrate / bitrates[0]);
        float bufferPenalty = -bolaV * (BolaBufferTarget - bufferLevel);
        return qualityUtility + bufferPenalty;
    }

    private float CalculateBolaV()
    {
        float qualityDiffSum = 0f;
        for (int i = 1; i < bitrates.Length; i++)
        {
            qualityDiffSum += Mathf.Log((float)bitrates[i] / bitrates[i - 1]);
        }
        return (float)bitrates.Length / (BolaBufferTarget * qualityDiffSum);
    }

    void Update()
    {
        float currentBufferLevel = 15f;

        int bolaBitrate = BolaABRDecision(currentBufferLevel);
    }
}
