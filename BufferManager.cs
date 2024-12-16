// Purpose: This script is used to manage the buffer of the video player. It keeps track of the segments that are currently in the buffer and removes the oldest segment when the buffer is full. The buffer size can be set to a specific value (N) and the current buffer size can be retrieved. The buffer queue can also be retrieved if needed. This script is a singleton and should be attached to an empty GameObject in the scene.
//Imports:
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

public class BufferManager : MonoBehaviour
{
    private static BufferManager instance;
    public static BufferManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject bufferManagerObject = new GameObject("BufferManager");
                instance = bufferManagerObject.AddComponent<BufferManager>();
            }
            return instance;
        }
    }

    private Queue<string> bufferQueue = new Queue<string>();
    private string buffermanagerLog = "";
    private int bufferCapacity = 2; // Default buffer capacity (changeable)
    private DateTime startTime;

    void Awake()
    {
        // Singleton pattern to keep only one instance of BufferManager
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            startTime = DateTime.Now;
        }
        else
        {
            Destroy(gameObject);
        }
        buffermanagerLog = Path.Combine(Application.persistentDataPath, "BufferManagerLog.txt");
    }

    // Set buffer size to a specific value (N)
    public void SetBufferSize(int size)
    {
        File.AppendAllText(buffermanagerLog, $"{GetTimestamp()} Buffer size set to: {size}\n");
        bufferCapacity = size;
    }

    public int GetMaxBufferSize()
    {
        return bufferCapacity;
    }

    // Return the current buffer size (number of elements in the buffer)
    public int GetBufferSize()
    {
        return bufferQueue.Count;
    }

    // Add a segment to the buffer
    public void AddToBuffer(string segmentPath)
    {
        if (bufferQueue.Count >= bufferCapacity)
        {
            File.AppendAllText(buffermanagerLog, $"{GetTimestamp()} Buffer is full. Cannot add segment: {segmentPath}\n");
            return;
        }

        bufferQueue.Enqueue(segmentPath);
        File.AppendAllText(buffermanagerLog, $"{GetTimestamp()} Segment added to buffer: {segmentPath}\n");
        File.AppendAllText(buffermanagerLog, $"{GetTimestamp()} Current buffer size: {bufferQueue.Count}\n");
        Debug.Log("Segment added to buffer. Current buffer size: " + bufferQueue.Count);
    }

    // Remove the oldest segment from the buffer
    public void RemoveFromBuffer( )
    {
        if (bufferQueue.Count > 0)
        {
            string removedSegment = bufferQueue.Dequeue();
            File.AppendAllText(buffermanagerLog, $"{GetTimestamp()} Removed segment from buffer: {removedSegment}\n");
            File.AppendAllText(buffermanagerLog, $"{GetTimestamp()} Current buffer size: {bufferQueue.Count}\n");
            Debug.Log("Removed segment from buffer: " + removedSegment);
        }
        else
        {
            File.AppendAllText(buffermanagerLog, $"{GetTimestamp()} Buffer is empty. Cannot remove segment.\n");
            Debug.Log("Buffer is empty. Cannot remove segment.");
        }
    }

    // Get the current buffer queue (optional method)
    public Queue<string> GetBufferQueue()
    {
        return bufferQueue;
    }

    // Get timestamp in seconds since start
    private string GetTimestamp()
    {
        TimeSpan elapsedTime = DateTime.Now - startTime;
        return $"[{elapsedTime.TotalSeconds:F2}s]";
    }
    public bool isSegmentFirstInBuffer(string path)
    {
        if (bufferQueue.Count > 0)
        {
            string firstSegment = bufferQueue.Peek();
            if(firstSegment.Contains(path))
            {
                File.AppendAllText(buffermanagerLog, $"{GetTimestamp()} Segment {path} is first in buffer\n");
            }
            else{
                File.AppendAllText(buffermanagerLog, $"{GetTimestamp()} Segment {path} is not first in buffer\n");
            }   
            return firstSegment.Contains(path);
        }
        return false;

    }
    
    public string GetFirstSegmentInBuffer()
    {
        if (bufferQueue.Count > 0)
        {
            return bufferQueue.Peek();
        }
        return "";
    }
    public void printQueue()
    {
        Debug.Log("-->>>> debug 19 Printing buffer queue");
        // create new queue to iterate over
        Queue<string> tempQueue = new Queue<string>(bufferQueue);
        while(tempQueue.Count > 0)
        {
            string segment = tempQueue.Dequeue();
            Debug.Log("---->>> debug 19 printing buffer : segment: " + segment);
        }
    }
}