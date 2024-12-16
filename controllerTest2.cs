using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;

public class InputTracker : MonoBehaviour
{
    private XRNode rightHand = XRNode.RightHand;
    private XRNode leftHand = XRNode.LeftHand;
    private string logFileName;
    private int fileIndex = 0;
    public int trackQueueSize = 0;

    private DashVideoPlayer dashVideoPlayer;

    private Queue<InputLogEntry> inputQueue = new Queue<InputLogEntry>();
    private bool waitingForInput = false;

    void Start()
    {
        dashVideoPlayer = gameObject.GetComponent<DashVideoPlayer>();
        fileIndex = GetNextLogFileIndex(); // Get the next available file index
        logFileName = $"controller_inputs{fileIndex}.txt"; // Set the initial log file name
        LogInput("InputTracker started.");
    }

    void Update()
    {
        // Other updates can go here if needed
    }

    public void SendImpulseAndTrackInput(int segmentNumber)
    {
        trackQueueSize += 1;
        if (trackQueueSize > 1)
        {
            LogInput("Already waiting for input. Adding segment to queue.");
            Debug.LogWarning("Already waiting for input. Adding segment to queue.");
            inputQueue.Enqueue(new InputLogEntry(segmentNumber));
            return;
        }
        StartCoroutine(WaitForButtonPressAndLog(segmentNumber));
    }

    private IEnumerator WaitForButtonPressAndLog(int segmentNumber)
    {
        
        LogInput($"Waiting for button press to log segment number {segmentNumber}...");

        // Send impulse to both controllers
        SendHapticImpulse(rightHand);
        SendHapticImpulse(leftHand);

        bool inputDetected = false;

        while (!inputDetected)
        {
            if (CheckForButtonPress(rightHand) || CheckForButtonPress(leftHand))
            {
                inputDetected = true;
                trackQueueSize -= 1;
                LogInput($"Segment number {segmentNumber} logged with button press.");

                // Log segment number and timestamp to "controller_inputsX.txt" file
                string logMessage = $"Timestamp: {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}, Segment Number: {segmentNumber}";
                File.AppendAllText(Path.Combine(Application.persistentDataPath, logFileName), logMessage + "\n");
            }
            else {
                yield return null;
            }
            
        }

        // Check if there are queued inputs
        Debug.Log("debug 19 ----->>>>> Queue size: " + inputQueue.Count);
        if (inputQueue.Count > 0)
        {
            InputLogEntry nextInput = inputQueue.Dequeue();
            Debug.Log("debug 19 ----->>>>> Queue size: " + inputQueue.Count);
            StartCoroutine(WaitForButtonPressAndLog(nextInput.segmentNumber));
        }
        else
        {
            Debug.Log("debug 19 ----->>>>> testing dash player instance laoded");
            Debug.Log("debug 19 ----->>>>> Dash testing segment number " + dashVideoPlayer.segmentNumber);
            trackQueueSize = 0;
            Debug.Log("debug 19 ----->>>>>Setting Queue size for dash player: " + inputQueue.Count);
            dashVideoPlayer.EndPlaybackAndCleanup();
        }

        
    }

    private bool CheckForButtonPress(XRNode hand)
    {
        List<InputFeatureUsage> features = new List<InputFeatureUsage>();
        InputDevice device = InputDevices.GetDeviceAtXRNode(hand);

        if (device.isValid)
        {
            device.TryGetFeatureUsages(features);

            foreach (var feature in features)
            {
                if (feature.type == typeof(bool))
                {
                    bool value;
                    if (device.TryGetFeatureValue(feature.As<bool>(), out value) && value)
                    {
                       if(feature.name!="IsTracked") { 
                            SendHapticImpulse(rightHand);
                            LogInput($"{hand} - {feature.name} pressed");
                            
                            return true;
                             } // Send haptic feedback on button press
                    }
                }
            }
        }
        return false;
    }

    private void SendHapticImpulse(XRNode hand)
    {
        InputDevice device = InputDevices.GetDeviceAtXRNode(hand);

        if (device.isValid)
        {
            HapticCapabilities capabilities;
            if (device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
            {
                uint channel = 0;
                device.SendHapticImpulse(channel, 0.85f, 0.4f); // Adjust intensity and duration as needed
                LogInput($"Haptic feedback sent to {hand}.");
            }
            else
            {
                LogInput($"Haptic feedback not supported for {hand}.");
            }
        }
        else
        {
            LogInput($"{hand} device is not valid for haptic feedback.");
        }
    }

    private void LogInput(string message)
    {
        Debug.Log("debug 19 --->>>> " + message);
        File.AppendAllText(Path.Combine(Application.persistentDataPath, logFileName), message + "\n");
    }

    private int GetNextLogFileIndex()
    {
        int index = 0;
        while (File.Exists(Path.Combine(Application.persistentDataPath, $"controller_inputs{index}.txt")))
        {
            index++;
        }
        return index;
    }

    private struct InputLogEntry
    {
        public int segmentNumber;

        public InputLogEntry(int segmentNumber)
        {
            this.segmentNumber = segmentNumber;
        }
    }
}