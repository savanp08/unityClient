using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;

public class InputTracker2 : MonoBehaviour
{
    private XRNode rightHand = XRNode.RightHand;
    private string logFileName;
    private int fileIndex = 0;

    void Start()
    {
        logFileName = GetUniqueLogFileName();
        LogInput("InputTracker started.");
    }

    void Update()
    {
        // Check for button presses and log them
        TrackInput(rightHand);
    }
    
    private void TrackInput(XRNode hand)
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
                    if (device.TryGetFeatureValue(feature.As<bool>(), out value))
                    {
                        if (value)
                        {
                            
                            if(feature.name!="IsTracked") { 
                            SendHapticFeedback(device);
                            LogInput($"{hand} - {feature.name} pressed");
                             } // Send haptic feedback on button press
                        }
                        else
                        {
                            
                        }
                    }
                    else
                    {
                        LogInput($"Failed to get feature value for {hand} - {feature.name}");
                    }
                }
            }
        }
        else
        {
            LogInput($"{hand} device is not valid.");
        }
    }

    private void LogInput(string message)
    {
        Debug.Log(message);
        File.AppendAllText(Path.Combine(Application.persistentDataPath, logFileName), message + "\n");
    }

    private void SendHapticFeedback(InputDevice device)
    {
       
        HapticCapabilities capabilities;
        if (device.TryGetHapticCapabilities(out capabilities) && capabilities.supportsImpulse)
        {
            uint channel = 0;
            device.SendHapticImpulse(channel, 0.5f, 0.1f); // Adjust intensity and duration as needed
            LogInput("Haptic feedback sent.");
        }
        else
        {
            LogInput("Haptic feedback not supported.");
        }
      
    }

    private string GetUniqueLogFileName()
    {
        string baseFileName = "InputControllers_logs";
        string extension = ".txt";
        string fileName = $"{baseFileName}{fileIndex}{extension}";

        while (File.Exists(Path.Combine(Application.persistentDataPath, fileName)))
        {
            fileIndex++;
            fileName = $"{baseFileName}{fileIndex}{extension}";
        }

        return fileName;
    }
}
