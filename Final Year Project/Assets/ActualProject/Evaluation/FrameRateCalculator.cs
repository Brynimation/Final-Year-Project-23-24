using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class FrameRateCalculator : MonoBehaviour
{
    BufferManager bufferManager;
    List<float> fpsValues;

    int currentChunkDistance;
    int currentRenderDistance;
    int currentMaxNoStars;
    int currentPlanetResolution;
    int currentStarResolution;

    bool isOdd;
    float fps1Low;
    float fps01Low;
    bool calculated = true;
    float maxFrameRate;
    float minFrameRate;
    float currentAverageFrameRate;
    string fileName = "fps.txt";
    string pathDir;
    int framesElapsed;
    int runCounter = 0;
    private void GetCurrentParams()
    {
        if (bufferManager == null) return;
        currentChunkDistance = bufferManager.chunkSize;
        currentRenderDistance = bufferManager.renderDistance;
        Debug.Log($"current render distance: {currentRenderDistance}");
        currentMaxNoStars = bufferManager.minMaxNumParticles[1];
        currentPlanetResolution = bufferManager.planetResolution;
        currentStarResolution = bufferManager.starResolution;
    }
    private void ReadDataFromFile()
    {

        // Check if the file exists
        if (File.Exists(pathDir))
        {
            // Read data from file
            isOdd = true; //only incnrease run counter for odd lines.
            using (StreamReader reader = new StreamReader(pathDir))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    // Check if the line is not null, empty, or consists only of white-space characters
                    if (!string.IsNullOrWhiteSpace(line) && isOdd)
                    {
                        runCounter++;
                    }
                    isOdd = !isOdd;
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

    private void TakeScreenCapture() 
    {
        string directory = Path.GetDirectoryName(pathDir);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
        string screenshotName = System.DateTime.Now.ToString("dd-MM-yyyy-HH-mm-ss") + currentPlanetResolution+ ".png";
        ScreenCapture.CaptureScreenshot(Path.Combine(directory, screenshotName), 2);
    }
    private void Awake()
    {
        pathDir = Path.Join(Application.dataPath, fileName);
        bufferManager = FindObjectOfType<BufferManager>();
        fpsValues = new List<float>();
        ReadDataFromFile();
    }
    void Start()
    {
        GetCurrentParams();
    }

    // Update is called once per frame
    void Update()
    {
        framesElapsed++;
        float curFps = 1.0f / Time.deltaTime;
        fpsValues.Add(curFps);

        //Debug.Log($"current fps {curFps}, average fps {currentAverageFrameRate}");
        currentAverageFrameRate += (curFps - currentAverageFrameRate) / framesElapsed;
        maxFrameRate = Mathf.Max(maxFrameRate, curFps);
        minFrameRate = (minFrameRate == 0.0) ? curFps : Mathf.Min(minFrameRate, curFps);

        fpsValues.Add(1.0f / Time.deltaTime);
        if (Input.GetKeyDown(KeyCode.F2)) 
        {
            TakeScreenCapture();
        }

    }
    

    void OnDestroy()
    {
        //calculate 1% low fps and 0.1% low fps
        int low1Count = Mathf.CeilToInt(fpsValues.Count * (0.01f));
        int low01Count = Mathf.CeilToInt(fpsValues.Count * (0.001f));
        List<float> sortedFPS = fpsValues.OrderBy(fps => fps).ToList();
        fps1Low = sortedFPS.Take(low1Count).Average();
        fps01Low = sortedFPS.Take(low01Count).Average();
        WriteDataToFile($"Run {runCounter}, Time: {System.DateTime.Now}. chunk size: {currentChunkDistance}, rendDist : {currentRenderDistance}, maxStars : {currentMaxNoStars}, starRes : {currentStarResolution}, planetRes: {currentPlanetResolution}"); 
        WriteDataToFile($"Frames Recorded: {framesElapsed}, Average fps: {currentAverageFrameRate}, minFps : {minFrameRate}, maxFps : {maxFrameRate}, 1% low: {fps1Low}, 0.1% low: {fps01Low}");
    }
}
