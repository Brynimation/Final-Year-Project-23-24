using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FrameRateCalculator : MonoBehaviour
{
    float prevTime;
    float maxFrameRate;
    float minFrameRate;
    float currentAverageFrameRate;
    string fileName = "fps.txt";
    string pathDir;
    int framesElapsed;
    int runCounter = 0;

    private void ReadDataFromFile()
    {
        // Check if the file exists
        if (File.Exists(pathDir))
        {
            // Read data from file
            using (StreamReader reader = new StreamReader(pathDir))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Check if the line is not null, empty, or consists only of white-space characters
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        runCounter++;
                    }
                }
            }
        }
    }
    private void WriteDataToFile(string data)
    {
        // Ensure the directory exists
        string directory = Path.GetDirectoryName(pathDir);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Write data to file
        using (StreamWriter writer = new StreamWriter(pathDir, true)) // true to append data to the file
        {
            writer.WriteLine(data);
        }

    }

    private void Awake()
    {
        pathDir = Path.Join(Application.dataPath, fileName);
        ReadDataFromFile();
    }
    void Start()
    {
        prevTime = Time.realtimeSinceStartup;

    }

    // Update is called once per frame
    void Update()
    {
        framesElapsed++;
        float timeNow = Time.realtimeSinceStartup;
        float timeSinceLastUpdate = timeNow - prevTime;
        float curFps = 1.0f / Time.deltaTime;
        currentAverageFrameRate += (curFps - currentAverageFrameRate) / framesElapsed;
        maxFrameRate = Mathf.Max(maxFrameRate, curFps);
        minFrameRate = (minFrameRate == 0.0) ? curFps :  Mathf.Min(minFrameRate, curFps);
        prevTime = timeNow;
    }

    private void OnDestroy()
    {
        WriteDataToFile($"Run {runCounter}: Average fps: {currentAverageFrameRate}, minFps : {minFrameRate}, maxFps : {maxFrameRate}");
    }
}
