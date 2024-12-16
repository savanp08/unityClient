using System;
using System.Collections.Generic;
using UnityEngine;

public class DataProcessor : MonoBehaviour
{
    // A method to clean the eye data by replacing any invalid values with the mean value for that column.
    public static List<Dictionary<string, object>> CleanEyeData(List<Dictionary<string, object>> eyeDataEntries)
    {
        // Calculate mean values for each column
        Dictionary<string, float> meanValues = CalculateMeanValues(eyeDataEntries);

        // Iterate through each eye data entry and replace invalid values with the mean
        foreach (var entry in eyeDataEntries)
        {
            foreach (var key in meanValues.Keys)
            {
                if (entry.ContainsKey(key))
                {
                    var value = entry[key];
                    if (value == null || string.IsNullOrEmpty(value.ToString()) || !float.TryParse(value.ToString(), out _))
                    {
                        // Replace invalid value with the column mean
                        entry[key] = meanValues[key];
                    }
                }
            }
        }

        return eyeDataEntries;
    }

    // Calculate the mean for each column in the eye data
    private static Dictionary<string, float> CalculateMeanValues(List<Dictionary<string, object>> eyeDataEntries)
    {
        Dictionary<string, float> sumValues = new Dictionary<string, float>();
        Dictionary<string, int> countValues = new Dictionary<string, int>();

        // Iterate through each entry to calculate sums and counts for each key
        foreach (var entry in eyeDataEntries)
        {
            foreach (var key in entry.Keys)
            {
                if (entry[key] != null && float.TryParse(entry[key].ToString(), out float value))
                {
                    if (!sumValues.ContainsKey(key))
                    {
                        sumValues[key] = 0f;
                        countValues[key] = 0;
                    }

                    sumValues[key] += value;
                    countValues[key]++;
                }
            }
        }

        // Calculate the mean for each key
        Dictionary<string, float> meanValues = new Dictionary<string, float>();
        foreach (var key in sumValues.Keys)
        {
            meanValues[key] = sumValues[key] / countValues[key];
        }

        return meanValues;
    }

    // Method to process data before feeding it to the model
    public static float[] ProcessDataForModel(List<Dictionary<string, object>> eyeDataEntries, float[] baseState)
    {
        // Clean the data to replace any invalid values with the mean
        List<Dictionary<string, object>> cleanedData = CleanEyeData(eyeDataEntries);

        // Combine base state with cleaned eye data
        List<float> combinedState = new List<float>(baseState);

        foreach (var data in cleanedData)
        {
            combinedState.Add(Convert.ToSingle(data["LeftEyeOpenness"]));
            combinedState.Add(Convert.ToSingle(data["RightEyeOpenness"]));
            combinedState.Add(Convert.ToSingle(data["LeftEyePupilDiameter"]));
            combinedState.Add(Convert.ToSingle(data["RightEyePupilDiameter"]));
            combinedState.Add(Convert.ToSingle(data["LeftEyePupilPositionX"]));
            combinedState.Add(Convert.ToSingle(data["LeftEyePupilPositionY"]));
            combinedState.Add(Convert.ToSingle(data["RightEyePupilPositionX"]));
            combinedState.Add(Convert.ToSingle(data["RightEyePupilPositionY"]));
            combinedState.Add(Convert.ToSingle(data["LeftEyeOriginX"]));
            combinedState.Add(Convert.ToSingle(data["LeftEyeOriginY"]));
            combinedState.Add(Convert.ToSingle(data["LeftEyeOriginZ"]));
            combinedState.Add(Convert.ToSingle(data["RightEyeOriginX"]));
            combinedState.Add(Convert.ToSingle(data["RightEyeOriginY"]));
            combinedState.Add(Convert.ToSingle(data["RightEyeOriginZ"]));
            combinedState.Add(Convert.ToSingle(data["CombinedEyeOriginX"]));
            combinedState.Add(Convert.ToSingle(data["CombinedEyeOriginY"]));
            combinedState.Add(Convert.ToSingle(data["CombinedEyeOriginZ"]));
            combinedState.Add(Convert.ToSingle(data["LeftEyeDirectionX"]));
            combinedState.Add(Convert.ToSingle(data["LeftEyeDirectionY"]));
            combinedState.Add(Convert.ToSingle(data["LeftEyeDirectionZ"]));
            combinedState.Add(Convert.ToSingle(data["RightEyeDirectionX"]));
            combinedState.Add(Convert.ToSingle(data["RightEyeDirectionY"]));
            combinedState.Add(Convert.ToSingle(data["RightEyeDirectionZ"]));
            combinedState.Add(Convert.ToSingle(data["CombinedEyeDirectionX"]));
            combinedState.Add(Convert.ToSingle(data["CombinedEyeDirectionY"]));
            combinedState.Add(Convert.ToSingle(data["CombinedEyeDirectionZ"]));
            combinedState.Add(Convert.ToSingle(data["HeadPosePositionX"]));
            combinedState.Add(Convert.ToSingle(data["HeadPosePositionY"]));
            combinedState.Add(Convert.ToSingle(data["HeadPosePositionZ"]));
            combinedState.Add(Convert.ToSingle(data["HeadPoseRotationX"]));
            combinedState.Add(Convert.ToSingle(data["HeadPoseRotationY"]));
            combinedState.Add(Convert.ToSingle(data["HeadPoseRotationZ"]));
            combinedState.Add(Convert.ToSingle(data["SegmentNumber"])); // Including the past segment number in the state
        }

        return combinedState.ToArray();
    }
}