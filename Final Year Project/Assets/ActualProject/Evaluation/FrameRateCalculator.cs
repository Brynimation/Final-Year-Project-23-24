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
    List<float> frameTimeValues;

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
    string summaryDataName = "fpsSummary.txt";
    string summaryPathDir;
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

    //File writiing code using tutorial: https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-write-text-to-a-file
    private void WriteDataToFile(string data, string pathDir)
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

    //Capture screenshot code: https://docs.unity3d.com/ScriptReference/ScreenCapture.CaptureScreenshot.html
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
        summaryPathDir = Path.Join(Application.dataPath, summaryDataName);
        if(!testing)bufferManager = FindObjectOfType<BufferManager>();
        fpsValues = new List<float>();
        frameTimeValues = new List<float>();
        ReadDataFromFile();
    }
    void Start()
    {
        GetCurrentParams();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            TakeScreenCapture();
        }
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

        fpsValues.Add(curFps);

        frameTimeValues.Add(Time.deltaTime);
        currentAverageFrameRate += (curFps - currentAverageFrameRate) / framesElapsed;
        maxFrameRate = Mathf.Max(maxFrameRate, curFps);
        minFrameRate = (minFrameRate == 0.0) ? curFps : Mathf.Min(minFrameRate, curFps);



    }

    void WriteFpsData() 
    {
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
            WriteDataToFile($"Frames Recorded: {framesElapsed}, Average fps: {currentAverageFrameRate}, minFps : {minFrameRate}, maxFps : {maxFrameRate}, 1% low: {fps1Low}, 0.1% low: {fps01Low}", pathDir);
            string fpsLow1Stringa = "";
            fps1LowData.ForEach(fps => { fpsLow1Stringa += $"{fps}, "; });
            string fpsLow01Stringa = "";
            fps01LowData.ForEach(fps => { fpsLow01Stringa += $"{fps}, "; });
            WriteDataToFile($"FPS1 low data: {fpsLow1Stringa}", pathDir);
            WriteDataToFile($"FPS01 low data: {fpsLow01Stringa}", pathDir);
            return;
        }


        WriteDataToFile($"Run {runCounter}, Time: {System.DateTime.Now}. chunk size: {currentChunkDistance}, rendDist : {currentRenderDistance}, maxStars : {currentMaxNoStars}, starRes : {currentStarResolution}, planetRes: {currentPlanetResolution}", pathDir);
        WriteDataToFile($"Frames Recorded: {framesElapsed}, Average fps: {currentAverageFrameRate}, minFps : {minFrameRate}, maxFps : {maxFrameRate}, 1% low: {fps1Low}, 0.1% low: {fps01Low}", pathDir);
    }

    public float CalculateStandardDeviation(List<float> values)
    {
        if (values.Count < 2) return 0.0f;

        // Calculate the average value
        float avg = values.Average();

        // Calculate the sum of the squared differences from the average
        float sumOfSquares = values.Sum(value => (value - avg) * (value - avg));

        // Calculate the standard deviation
        return Mathf.Sqrt(sumOfSquares / (values.Count - 1));
    }

    void WriteFrameTimeData() 
    {
        frameTimeValues.RemoveAll(t => t == 0.0);
        List<float> sortedTimes = frameTimeValues.OrderBy(t => t).ToList();
        int p99Count = Mathf.CeilToInt(frameTimeValues.Count * (0.01f));
        int p999Count = Mathf.CeilToInt(frameTimeValues.Count * (0.001f));
        List<float> sortedTimes99 = sortedTimes.Skip(frameTimeValues.Count - p99Count).ToList(); ;
        List<float> sortedTimes999 = sortedTimes.Skip(frameTimeValues.Count - p999Count).ToList(); ;
        float average = sortedTimes.Average();
        float average99 = sortedTimes99.Average();
        float average999 = sortedTimes999.Average();

        float median = sortedTimes[frameTimeValues.Count / 2];
        float median99 = sortedTimes99[p99Count / 2];
        float median999 = sortedTimes999[p999Count / 2];

        float standardDeviation = CalculateStandardDeviation(sortedTimes);
        float standardDeviation99 = CalculateStandardDeviation(sortedTimes99);
        float standardDeviation999 = CalculateStandardDeviation(sortedTimes999);
        WriteDataToFile($"Time: {System.DateTime.Now}. chunk size: {currentChunkDistance}, rendDist : {currentRenderDistance}, maxStars : {currentMaxNoStars}, starRes : {currentStarResolution}, planetRes: {currentPlanetResolution}", pathDir);
        WriteDataToFile($"Frames Recorded: {frameTimeValues.Count}, Average frametime: {average}, 99% high: {average99}, 99.9% high: {average999}", pathDir);
        WriteDataToFile($"Median: {median}, median99: { median99}, median 999: {median999}, std: {standardDeviation}, std99: {standardDeviation99}, std999: {standardDeviation999}", pathDir);

        WriteDataToFile($"Time: {System.DateTime.Now}. chunk size: {currentChunkDistance}, rendDist : {currentRenderDistance}, maxStars : {currentMaxNoStars}, starRes : {currentStarResolution}, planetRes: {currentPlanetResolution}", summaryPathDir);
        WriteDataToFile($"Frames Recorded: {frameTimeValues.Count}, Average frametime: {average}, 99% high: {average99}, 99.9% high: {average999}", summaryPathDir);
        WriteDataToFile($"Median: {median}, median99: {median99}, median 999: {median999}, std: {standardDeviation}, std99: {standardDeviation99}, std999: {standardDeviation999}", summaryPathDir);

        string fts = "";
        sortedTimes.ForEach(ft=> { fts += (ft > 0.0) ? $"{ft}, " : ""; });
        string unsortedTimes = "";
        frameTimeValues.ForEach(t => { unsortedTimes += (t > 0.0) ? $"{t}, " : ""; });
        WriteDataToFile("SORTED", pathDir);
        WriteDataToFile(fts, pathDir);
        WriteDataToFile("UNSORTED", pathDir);
        WriteDataToFile(unsortedTimes, pathDir);
        WriteDataToFile(" ", pathDir);
    }
    void OnDestroy()
    {
        WriteFrameTimeData();

    }
}
