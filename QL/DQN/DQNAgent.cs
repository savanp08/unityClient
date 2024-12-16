using Unity.Barracuda;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

public class DQNAgent : MonoBehaviour
{
    public NNModel modelAsset;          // The pre-trained model
    private Model model;
    private IWorker worker;
    private EyeQoEMetricsLogger eyeQoEMetricsLogger;
    private int segmentIndex;
    private List<int> pastActions;      // Store past actions to use in each segment's decision-making

    public int initialRandomSegments = 5;   // Number of initial segments handled by SegmentFetcher
    private string logFilePath;

    void Start()
    {
        logFilePath = Path.Combine(Application.persistentDataPath, "MLDebug.txt");
        LogToFile("Initializing DQNAgent");

        model = ModelLoader.Load(modelAsset);
        worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, model);
        
        eyeQoEMetricsLogger = FindObjectOfType<EyeQoEMetricsLogger>();
        segmentIndex = initialRandomSegments;  // Start DQN predictions from segment x+1
        pastActions = new List<int>();
    }

    // Selects action based on the model prediction, starting after the first x segments.
    public int SelectAction(int bufferSize)
    {
        int action;
        int segmentNumber = segmentIndex; // Use the segment index as the segment number

        LogToFile($"Selecting action for segment {segmentNumber} with buffer size {bufferSize}");

        // Begin making decisions only after the initial randomized segments handled by SegmentFetcher
        if (segmentNumber < initialRandomSegments)
        {
            // If segment is in the initial randomized phase, return a random action
            action = UnityEngine.Random.Range(0, 6);  // Assuming 6 possible actions (adjust based on your model)
            LogToFile($"Random action selected: {action}");
            Debug.Log($"Random action selected: {action}");
            pastActions.Add(action);
        }
        else
        {
            // After initial randomization, use the model to predict the next action
            action = GetActionFromModel(bufferSize, segmentNumber - 1);  // Use the past segment number
            LogToFile($"Model action selected: {action}");
            Debug.Log($"Model action selected: {action}");
            pastActions.Add(action);
        }

        segmentIndex++;  // Move to the next segment after processing
        return action;
    }

    // Fetch the eye data for the corresponding segment and use it along with the buffer size to make a decision.
    private int GetActionFromModel(int bufferSize, int segmentNumber)
    {
        LogToFile($"Fetching eye data for segment {segmentNumber}");

        // Fetch corresponding eye data for the segment from EyeQoEMetricsLogger
        var segmentData = eyeQoEMetricsLogger.GetSegmentData((int)segmentNumber);

        if (segmentData != null && segmentData.EyeDataEntries != null)
        {
            LogToFile($"Eye data found for segment {segmentNumber}");
            // Clean the eye data before use
            var cleanedEyeData = DataProcessor.CleanEyeData(segmentData.EyeDataEntries);

            // Combine the buffer size and other state data with the eye data
            float[] baseState = new float[] { bufferSize };  // Assuming buffer size is part of the state
            float[] combinedState = CombineStateWithEyeData(baseState, cleanedEyeData);
            
            // Use the DQN model to predict an action
            int action = PredictAction(combinedState);
            LogToFile($"Predicted action for segment {segmentNumber}: {action}");
            return action;
        }
        else
        {
            string warningMessage = $"Eye data for segment {segmentNumber} is not ready.";
            LogToFile(warningMessage);
            Debug.LogWarning(warningMessage);
            return -1; // Fallback action or error handling
        }
    }

    // Combine base state (e.g., buffer size) with eye data
    private float[] CombineStateWithEyeData(float[] baseState, List<Dictionary<string, object>> cleanedEyeData)
    {
        List<float> combinedData = new List<float>(baseState);

        foreach (var data in cleanedEyeData)
        {
            // Extract relevant eye data and add to the combined state
            combinedData.Add(Convert.ToSingle(data["LeftEyeOpenness"]));
            combinedData.Add(Convert.ToSingle(data["RightEyeOpenness"]));
            combinedData.Add(Convert.ToSingle(data["LeftEyePupilDiameter"]));
            combinedData.Add(Convert.ToSingle(data["RightEyePupilDiameter"]));
            combinedData.Add(Convert.ToSingle(data["SegmentNumber"])); // Including the past segment number in the state
            // Add other relevant eye data fields as needed
        }

        LogToFile("Combined state with eye data: " + string.Join(", ", combinedData));
        return combinedData.ToArray();
    }

    // Predict an action using the DQN model based on the combined state
    private int PredictAction(float[] state)
    {
        LogToFile("Predicting action based on state: " + string.Join(", ", state));

        try
        {
            Tensor inputTensor = new Tensor(1, state.Length, state);
            worker.Execute(inputTensor);
            Tensor outputTensor = worker.PeekOutput("output");

            int action = GetBestAction(outputTensor);

            LogToFile($"Predicted action: {action}");
            inputTensor.Dispose();
            outputTensor.Dispose();

            return action;
        }
        catch (Exception ex)
        {
            LogToFile("Error during model prediction: " + ex.Message);
            Debug.LogWarning("Model prediction failed due to mismatch in weights or data dimensions. Selecting random action.");
            return UnityEngine.Random.Range(0, 6);  // Assuming 6 possible actions (adjust based on your model)
        }
    }

    // Get the best action (the one with the highest Q-value) from the model's output
    private int GetBestAction(Tensor outputTensor)
    {
        int bestAction = 0;
        float maxQValue = outputTensor[0];

        for (int i = 1; i < outputTensor.length; i++)
        {
            if (outputTensor[i] > maxQValue)
            {
                maxQValue = outputTensor[i];
                bestAction = i;
            }
        }

        LogToFile($"Best action determined: {bestAction} with Q-value: {maxQValue}");
        return bestAction;
    }

    private void OnDestroy()
    {
        LogToFile("Disposing worker");
        worker.Dispose();
    }

    // Log messages to both a file and Debug.Log
    private void LogToFile(string message)
    {
        string logMessage = $"{DateTime.Now}: {message}";
        Debug.Log(logMessage);
        using (StreamWriter writer = new StreamWriter(logFilePath, true))
        {
            writer.WriteLine(logMessage);
        }
    }
}
