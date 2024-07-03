using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.XR;

public class InputTracker2 : MonoBehaviour
{
    private InputDevice rightController;
    private InputDevice leftController;
    private string logFileName;
    private int fileIndex = 0;
    private float hapticInterval = 5f;
    private float nextHapticTime;
    private List<InputDevice> devices;

    void Start()
    {
        devices = new List<InputDevice>();
        InputDevices.GetDevices(devices);
        LogMessage("---->>>>>> Devices Count: " + devices.Count);
        foreach (var device in devices)
        {
            LogMessage(device.name + " " + device.characteristics);
        }

        foreach (var device in devices)
        {
            if (device.characteristics.HasFlag(InputDeviceCharacteristics.Right))
            {
                rightController = device;
                LogMessage("Right Controller found: " + rightController.name);
            }
            else if (device.characteristics.HasFlag(InputDeviceCharacteristics.Left))
            {
                leftController = device;
                LogMessage("Left Controller found: " + leftController.name);
            }
            Debug.Log("Device: " + device.name + " " + device.characteristics);
        }

        logFileName = GetUniqueLogFileName();
        nextHapticTime = Time.time + hapticInterval;
    }

    void Update()
    {
        if (rightController.isValid)
        {
            LogControllerInput(rightController);
        }
        else
        {
            LogMessage("Right controller is not valid.");
        }

        if (leftController.isValid)
        {
            LogControllerInput(leftController);
        }
        else
        {
            LogMessage("Left controller is not valid.");
        }

        // Send haptic feedback every 5 seconds
        if (Time.time >= nextHapticTime)
        {
            SendPeriodicHapticFeedback();
            nextHapticTime = Time.time + hapticInterval;
        }
    }

    private void LogControllerInput(InputDevice controller)
    {
        bool anyButtonPressed = false;

        if (controller.TryGetFeatureValue(CommonUsages.trigger, out float triggerValue) && triggerValue > 0)
        {
            LogInput($"{controller.name} Trigger Value: " + triggerValue);
            anyButtonPressed = true;
        }
        if (controller.TryGetFeatureValue(CommonUsages.grip, out float gripValue) && gripValue > 0)
        {
            LogInput($"{controller.name} Grip Value: " + gripValue);
            anyButtonPressed = true;
        }
        if (controller.TryGetFeatureValue(CommonUsages.primaryButton, out bool primaryButtonValue) && primaryButtonValue)
        {
            LogInput($"{controller.name} Primary Button Value: " + primaryButtonValue);
            anyButtonPressed = true;
        }
        if (controller.TryGetFeatureValue(CommonUsages.secondaryButton, out bool secondaryButtonValue) && secondaryButtonValue)
        {
            LogInput($"{controller.name} Secondary Button Value: " + secondaryButtonValue);
            anyButtonPressed = true;
        }
        if (controller.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 primary2DAxisValue) && primary2DAxisValue != Vector2.zero)
        {
            LogInput($"{controller.name} Primary 2D Axis Value: " + primary2DAxisValue);
            anyButtonPressed = true;
        }
        if (controller.TryGetFeatureValue(CommonUsages.secondary2DAxis, out Vector2 secondary2DAxisValue) && secondary2DAxisValue != Vector2.zero)
        {
            LogInput($"{controller.name} Secondary 2D Axis Value: " + secondary2DAxisValue);
            anyButtonPressed = true;
        }
        if (controller.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 devicePositionValue))
        {
            LogInput($"{controller.name} Device Position Value: " + devicePositionValue);
        }
        if (controller.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion deviceRotationValue))
        {
            LogInput($"{controller.name} Device Rotation Value: " + deviceRotationValue);
        }
        if (controller.TryGetFeatureValue(CommonUsages.isTracked, out bool isTrackedValue))
        {
            LogInput($"{controller.name} Is Tracked Value: " + isTrackedValue);
        }
        if (controller.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 deviceVelocityValue))
        {
            LogInput($"{controller.name} Device Velocity Value: " + deviceVelocityValue);
        }
        if (controller.TryGetFeatureValue(CommonUsages.deviceAngularVelocity, out Vector3 deviceAngularVelocityValue))
        {
            LogInput($"{controller.name} Device Angular Velocity Value: " + deviceAngularVelocityValue);
        }
        if (controller.TryGetFeatureValue(CommonUsages.deviceAcceleration, out Vector3 deviceAccelerationValue))
        {
            LogInput($"{controller.name} Device Acceleration Value: " + deviceAccelerationValue);
        }

        // Check and log if any buttons are pressed
        if (anyButtonPressed)
        {
            SendHapticFeedback(controller);
        }

        TrackInput(controller);
    }

    private void TrackInput(InputDevice device)
    {
        List<InputFeatureUsage> features = new List<InputFeatureUsage>();
        device.TryGetFeatureUsages(features);

        foreach (var feature in features)
        {
            if (feature.type == typeof(bool))
            {
                bool value;
                if (device.TryGetFeatureValue(feature.As<bool>(), out value) && value)
                {
                    LogInput($"---->>>>>>>>>> {device.name} - {feature.name} pressed");
                    SendHapticFeedback(device);
                }
            }
        }
    }

    private void LogInput(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logFileName, message + "\n");
    }

    private void SendHapticFeedback(InputDevice controller)
    {
        controller.SendHapticImpulse(0, 0.5f, 0.1f); // Adjust intensity and duration as needed
    }

    private void SendPeriodicHapticFeedback()
    {
        if (rightController.isValid)
        {
            rightController.SendHapticImpulse(0, 0.5f, 0.1f);
        }
        if (leftController.isValid)
        {
            leftController.SendHapticImpulse(0, 0.5f, 0.1f);
        }
    }

    private void LogMessage(string message)
    {
        Debug.Log(message);
        File.AppendAllText(logFileName, message + "\n");
    }

    private string GetUniqueLogFileName()
    {
        string baseFileName = "InputControllers_logs";
        string extension = ".txt";
        string fileName = $"{baseFileName}{fileIndex}{extension}";

        while (File.Exists(fileName))
        {
            fileIndex++;
            fileName = $"{baseFileName}{fileIndex}{extension}";
        }

        return fileName;
    }
}
