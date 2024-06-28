using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class BufferManager : MonoBehaviour
{
    private static BufferManager instance;

    public static BufferManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("BufferManager");
                instance = obj.AddComponent<BufferManager>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private List<string> bufferQueue = new List<string>();

    // Constructor should be private to prevent instantiation
    private BufferManager() { }

    // Ensure InitializeBufferQueue is public
    public void InitializeBufferQueue()
    {
        string bufferQueueFolder = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos");
if(GetPlatformCode() == 'W' || GetPlatformCode() == 'A'){
            bufferQueueFolder = Path.Combine(Application.persistentDataPath, "Videos");
        }
        if (!Directory.Exists(bufferQueueFolder))
        {
            Directory.CreateDirectory(bufferQueueFolder);
        }
    }

    public void AddToBuffer(string filePath)
    {
        bufferQueue.Add(filePath);
    }

    public bool IsSegmentAvailable(int segmentNumber)
    {

       string filePath = Path.Combine("/home/mobisec/Desktop/optiplex/pensive-PyTorch-Temp/temp/Videos", "segment" + segmentNumber + ".webm");
       if(GetPlatformCode() == 'W' || GetPlatformCode() == 'A'){
            filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");
        }
        return File.Exists(filePath);
    }

    public string GetNextSegment()
    {
        if (bufferQueue.Count > 0)
        {
            string nextSegment = bufferQueue[0];
            bufferQueue.RemoveAt(0);
            return nextSegment;
        }
        return null;
    }

    public List<string> GetBufferQueue()
    {
        return bufferQueue;
    }

    public bool HasSegments()
    {
        return bufferQueue.Count > 0;
    }

    
     public static char GetPlatformCode()
    {
        #if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        return 'W'; // Windows
        #elif UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX
        return 'L'; // Linux
        #elif UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
        return 'M'; // macOS
        #elif UNITY_ANDROID
        return 'A'; // Android
        #elif UNITY_IOS
        return 'I'; // iOS
        #else
        return 'U'; // Unknown or unsupported platform
        #endif
    }
}


// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;

// public class BufferManager : MonoBehaviour
// {
//     private static BufferManager instance;

//     public static BufferManager Instance
//     {
//         get
//         {
//             if (instance == null)
//             {
//                 GameObject obj = new GameObject("BufferManager");
//                 instance = obj.AddComponent<BufferManager>();
//                 DontDestroyOnLoad(obj);
//             }
//             return instance;
//         }
//     }

//     private List<string> bufferQueue = new List<string>();

//     // Constructor should be private to prevent instantiation
//     private BufferManager() { }

//     // Ensure InitializeBufferQueue is public
//     public void InitializeBufferQueue()
//     {
//         string bufferQueueFolder = Path.Combine(Application.persistentDataPath);
//         if (!Directory.Exists(bufferQueueFolder))
//         {
//             Directory.CreateDirectory(bufferQueueFolder);
//         }
//     }

//     public void AddToBuffer(string filePath)
//     {
//         bufferQueue.Add(filePath);
//     }

//     public bool IsSegmentAvailable(int segmentNumber)
//     {
//         string filePath = Path.Combine(Application.persistentDataPath, "segment" + segmentNumber + ".mp4");
//         return File.Exists(filePath);
//     }

//     public string GetNextSegment()
//     {
//         if (bufferQueue.Count > 0)
//         {
//             string nextSegment = bufferQueue[0];
//             bufferQueue.RemoveAt(0);
//             return nextSegment;
//         }
//         return null;
//     }

//     public List<string> GetBufferQueue()
//     {
//         return bufferQueue;
//     }
// }
