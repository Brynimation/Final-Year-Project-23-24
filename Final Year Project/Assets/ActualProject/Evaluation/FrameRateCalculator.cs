using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    public bool testing = false;
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
    bool startRecording = false;
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
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 61;
        pathDir = Path.Join(Application.dataPath, fileName);
        if(!testing)bufferManager = FindObjectOfType<BufferManager>();
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
        if (Input.GetKeyDown(KeyCode.F1))
        {
            SceneManager.LoadScene(0);
        }
        if (Input.GetKeyDown(KeyCode.R)) 
        {
            startRecording = true;
        }
        if (!startRecording) return;
        framesElapsed++;
        float curFps = 1.0f / Time.deltaTime;
        /*if (curFps < 30.0f) 
        {
            Debug.Log($"Cur fps is: {curFps}");
        }*/
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
        Debug.Log($"low count 1: {low1Count}, low count 01: {low01Count}");
        List<float> sortedFPS = fpsValues.OrderBy(fps => fps).ToList();
        List<float> fps1LowData = sortedFPS.Take(low1Count).ToList();
        List<float> fps01LowData = sortedFPS.Take(low01Count).ToList();
        fps1Low = sortedFPS.Take(low1Count).Average();
        fps01Low = sortedFPS.Take(low01Count).Average();
        if (testing) 
        {
            WriteDataToFile($"Frames Recorded: {framesElapsed}, Average fps: {currentAverageFrameRate}, minFps : {minFrameRate}, maxFps : {maxFrameRate}, 1% low: {fps1Low}, 0.1% low: {fps01Low}");
            string fpsLow1Stringa = "";
            fps1LowData.ForEach(fps => { fpsLow1Stringa += $"{fps}, "; });
            string fpsLow01Stringa = "";
            fps01LowData.ForEach(fps => { fpsLow01Stringa += $"{fps}, "; });
            WriteDataToFile($"FPS1 low data: {fpsLow1Stringa}");
            WriteDataToFile($"FPS01 low data: {fpsLow01Stringa}");
            return;
        }


        WriteDataToFile($"Run {runCounter}, Time: {System.DateTime.Now}. chunk size: {currentChunkDistance}, rendDist : {currentRenderDistance}, maxStars : {currentMaxNoStars}, starRes : {currentStarResolution}, planetRes: {currentPlanetResolution}"); 
        WriteDataToFile($"Frames Recorded: {framesElapsed}, Average fps: {currentAverageFrameRate}, minFps : {minFrameRate}, maxFps : {maxFrameRate}, 1% low: {fps1Low}, 0.1% low: {fps01Low}");
        /*string fpsLow1String = "";
        fps1LowData.ForEach(fps => { fpsLow1String += $"{fps}, "; });
        string fpsLow01String = "";
        fps01LowData.ForEach(fps => { fpsLow01String += $"{fps}, "; });
        WriteDataToFile($"FPS1 low data: {fpsLow1String}");
        WriteDataToFile($"FPS01 low data: {fpsLow01String}");
        */
    }
}
