using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR;
using Wave.Essence;
using Wave.Essence.Eye;
using static Wave.Essence.Eye.EyeManager;

public class InteractionDataCapturer : MonoBehaviour
{
    private Thread collectInteractionDataThread;
    private volatile bool shouldStop = false;
    private readonly object lockObject = new object();

    public EyeManager eyeManager;

    public const float eyeRayDistance = 2000;

    private int fpsCounter = 0;
    private ulong sequenceNumber = 0;
    private Stopwatch stopwatch;

    private string eyeDataPersistentPath;
    private string operationDataPersistentPath;
    private string interactionDataPersistentPath;

    private InputDevice rightControllerInputDevice;
    private InputDevice leftControllerInputDevice;

    private Dictionary<string, object> eyeData;
    private Dictionary<string, object> operationData;
    private string eyeDataJson;
    private string operationDataJson;
    private StringBuilder interactionDataSB;

    // Eye data
    private bool isEyeTrackingAvailable, hasEyeTrackingData;
    private Vector3 combinedEyeOrigin, leftEyeOrigin, rightEyeOrigin;
    private Vector3 combinedEyeDirection, leftEyeDirection, rightEyeDirection;
    private float leftEyeOpenness, rightEyeOpenness, leftEyePupilDiameter, rightEyePupilDiameter;
    private Vector2 leftEyePupilPositionInSensorArea, rightEyePupilPositionInSensorArea;

    // Controller Operation data
    private bool rightTrigger, rightGrip, rightA, rightB;
    private bool leftTrigger, leftGrip, leftX, leftY;
    private Vector2 rightJoystick, leftJoystick;

    public class SerializableVector3
    {
        public float x;
        public float y;
        public float z;
        public SerializableVector3(Vector3 vector)
        {
            x = vector.x;
            y = vector.y;
            z = vector.z;
        }
    }

    public class SerializableVector2
    {
        public float x;
        public float y;
        public SerializableVector2(Vector2 vector)
        {
            x = vector.x;
            y = vector.y;
        }
    }

    private void Awake()
    {
        eyeManager = GameObject.Find("EyeManager").GetComponent<EyeManager>();
        stopwatch = Stopwatch.StartNew();
    }

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 60;

        System.Net.ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;

        eyeDataPersistentPath = Path.Combine(Application.persistentDataPath, "eyeMovementData.txt");
        operationDataPersistentPath = Path.Combine(Application.persistentDataPath, "operationData.txt");

        interactionDataPersistentPath = Path.Combine(Application.persistentDataPath, "interactionData.txt");
        eyeDataPersistentPath = interactionDataPersistentPath;
        operationDataPersistentPath = interactionDataPersistentPath;

        interactionDataSB = new StringBuilder();

        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission("android.permission.WRITE_EXTERNAL_STORAGE"))
        {
            UnityEngine.Android.Permission.RequestUserPermission("android.permission.WRITE_EXTERNAL_STORAGE");
        }

        rightControllerInputDevice = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        leftControllerInputDevice = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        collectInteractionDataThread = new Thread(CollectInteractionDataThreadFunction);
        collectInteractionDataThread.Start();
    }

    private void CollectInteractionDataThreadFunction()
    {
        while (!shouldStop)
        {
            UnityEngine.Debug.Log("CollectInteractionDataThreadFunction is running, stopwatch: " + stopwatch.Elapsed.TotalMilliseconds.ToString() + "ms.");
            string interactionDataString = CollectInteractionData();
            AppendInteractionDataSB(interactionDataString);

            Thread.Sleep(1000 / 150);
        }
    }

    private void OnDestroy()
    {
        shouldStop = true;
        collectInteractionDataThread.Join();
    }

    // Update is called once per frame
    void Update()
    {
        if (fpsCounter % 200 == 199)
        {
            string interactionDataString = RetrieveAndClearInteractionDataSB();
            StartCoroutine(StoreData(interactionDataString));
            fpsCounter = 0;
        }
        fpsCounter++;
    }

    public void AppendInteractionDataSB(string data)
    {
        lock (lockObject)
        {
            interactionDataSB.Append(data);
        }
    }

    public string RetrieveAndClearInteractionDataSB()
    {
        lock (lockObject)
        {
            string results = interactionDataSB.ToString();
            interactionDataSB.Clear();
            return results;
        }
    }

    private string CollectInteractionData()
    {
        // collect eye and operation data
        eyeDataJson = CollectEyeData();
        operationDataJson = CollectOperationData();
        sequenceNumber++;

        return eyeDataJson + '\n' + operationDataJson + "\n\n";
    }

    IEnumerator StoreData(string data)
    {
        SaveInteractionData(data);
        yield return null;
    }

    private string CollectEyeData()
    {
        eyeData = new Dictionary<string, object>();

        // timestamp and sequence number to sort the order of data package
        eyeData["TimeStamp"] = stopwatch.Elapsed.TotalMilliseconds;
        eyeData["SequenceNumber"] = sequenceNumber;
        eyeData["DataType"] = "Eye";

        // Eye data status
        eyeData["EyeTrackingStatus"] = eyeManager.GetEyeTrackingStatus();
        isEyeTrackingAvailable = eyeManager.IsEyeTrackingAvailable();
        eyeData["IsEyeTrackingAvailable"] = isEyeTrackingAvailable;
        hasEyeTrackingData = eyeManager.HasEyeTrackingData();
        eyeData["HasEyeTrackingData"] = hasEyeTrackingData;

        if (eyeManager.GetEyeOrigin(EyeType.Combined, out combinedEyeOrigin))
        {
            eyeData["CombinedEyeOrigin"] = new SerializableVector3(combinedEyeOrigin);
        }
        if (eyeManager.GetEyeDirectionNormalized(EyeType.Combined, out combinedEyeDirection))
        {
            eyeData["CombinedEyeDirection"] = new SerializableVector3(combinedEyeDirection);
        }
        if (eyeManager.GetEyeOrigin(EyeType.Left, out leftEyeOrigin))
        {
            eyeData["LeftEyeOrigin"] = new SerializableVector3(leftEyeOrigin);
        }
        if (eyeManager.GetEyeDirectionNormalized(EyeType.Left, out leftEyeDirection))
        {
            eyeData["LeftEyeDirection"] = new SerializableVector3(leftEyeDirection);
        }
        if (eyeManager.GetLeftEyeOpenness(out leftEyeOpenness))
        {
            eyeData["LeftEyeOpenness"] = leftEyeOpenness;
        }
        if (eyeManager.GetLeftEyePupilDiameter(out leftEyePupilDiameter))
        {
            eyeData["LeftEyePupilDiameter"] = leftEyePupilDiameter;
        }
        if (eyeManager.GetLeftEyePupilPositionInSensorArea(out leftEyePupilPositionInSensorArea))
        {
            eyeData["LeftEyePupilPositionInSensorArea"] = new SerializableVector2(leftEyePupilPositionInSensorArea);
        }
        if (eyeManager.GetEyeOrigin(EyeType.Right, out rightEyeOrigin))
        {
            eyeData["RightEyeOrigin"] = new SerializableVector3(rightEyeOrigin);
        }
        if (eyeManager.GetEyeDirectionNormalized(EyeType.Right, out rightEyeDirection))
        {
            eyeData["RightEyeDirection"] = new SerializableVector3(rightEyeDirection);
        }
        if (eyeManager.GetRightEyeOpenness(out rightEyeOpenness))
        {
            eyeData["RightEyeOpenness"] = rightEyeOpenness;
        }
        if (eyeManager.GetRightEyePupilDiameter(out rightEyePupilDiameter))
        {
            eyeData["RightEyePupilDiameter"] = rightEyePupilDiameter;
        }
        if (eyeManager.GetRightEyePupilPositionInSensorArea(out rightEyePupilPositionInSensorArea))
        {
            eyeData["RightEyePupilPositionInSensorArea"] = new SerializableVector2(rightEyePupilPositionInSensorArea);
        }

        // Convert the dictionary to a JSON string
        string jsonData = JsonConvert.SerializeObject(eyeData, Formatting.Indented);

        // Output the JSON string
        UnityEngine.Debug.Log(jsonData);
        return jsonData;
    }

    private string CollectOperationData()
    {
        operationData = new Dictionary<string, object>();

        // timestamp and sequence number to sort the order of data package
        operationData["TimeStamp"] = stopwatch.Elapsed.TotalMilliseconds;
        operationData["SequenceNumber"] = sequenceNumber;
        operationData["DataType"] = "Operation";

        if (rightControllerInputDevice != null)
        {
            if (rightControllerInputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out rightTrigger))
            {
                operationData["rightTrigger"] = rightTrigger;
            }
            if (rightControllerInputDevice.TryGetFeatureValue(CommonUsages.gripButton, out rightGrip))
            {
                operationData["rightGrip"] = rightGrip;
            }
            if (rightControllerInputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out rightA))
            {
                operationData["rightA"] = rightA;
            }
            if (rightControllerInputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out rightB))
            {
                operationData["rightB"] = rightB;
            }
            if (rightControllerInputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out rightJoystick))
            {
                operationData["rightJoystick"] = new SerializableVector2(rightJoystick);
            }
        }

        if (leftControllerInputDevice != null)
        {
            if (leftControllerInputDevice.TryGetFeatureValue(CommonUsages.triggerButton, out leftTrigger))
            {
                operationData["leftTrigger"] = leftTrigger;
            }
            if (leftControllerInputDevice.TryGetFeatureValue(CommonUsages.gripButton, out leftGrip))
            {
                operationData["leftGrip"] = leftGrip;
            }
            if (leftControllerInputDevice.TryGetFeatureValue(CommonUsages.primaryButton, out leftX))
            {
                operationData["leftA"] = leftX;
            }
            if (leftControllerInputDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out leftY))
            {
                operationData["leftB"] = leftY;
            }
            if (leftControllerInputDevice.TryGetFeatureValue(CommonUsages.primary2DAxis, out leftJoystick))
            {
                operationData["leftJoystick"] = new SerializableVector2(leftJoystick);
            }
        }

        // Convert the dictionary to a JSON string
        string jsonData = JsonConvert.SerializeObject(operationData, Formatting.Indented);

        // Output the JSON string
        UnityEngine.Debug.Log(jsonData);
        return jsonData;
    }

    private void SaveInteractionData(string interactionData)
    {
        try
        {
            File.AppendAllText(interactionDataPersistentPath, interactionData + "\n");
            UnityEngine.Debug.Log("Interaction data saved! Path:" + interactionDataPersistentPath);
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.Log("Failed to save interaction data: " + e.Message);
        }
    }

    public class AcceptAllCertificates : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // Simply return true to accept all certificates
            return true;
        }
    }

    public bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
    {
        // TODO: hardcode to avoid the SSL CA certificate.
        return true;
    }
}
