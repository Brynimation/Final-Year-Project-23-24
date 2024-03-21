using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class UIMenu : MonoBehaviour
{

    [SerializeField] Material skyboxMat;
    [SerializeField] Slider renderDistanceSlider;
    [SerializeField] TMP_Text renderDistanceText;

    [SerializeField] Slider starCountSlider;
    [SerializeField] TMP_Text starCountText;

    [SerializeField] Slider planetResSlider;
    [SerializeField] TMP_Text planetResText;

    [SerializeField] Slider starResSlider;
    [SerializeField] TMP_Text starResText;
    [SerializeField] GameObject instructionsWindow;
    [SerializeField] Button HowTo;
    [SerializeField] Button Quit;
    [SerializeField] Button closeWindow;

    [Range(0, 0.5f)]
    public float starSpeed;
    public static int renderDistance;
    public static int starCount;
    public static int planetRes;
    public static int starRes;
    public static bool useChosenSettings = true;


    [SerializeField] Button ExploreButton;
    void SetVariableAndText(ref int variable, ref TMP_Text text, int value) 
    {
        variable = value;
        text.SetText(variable.ToString());
    }
    void SetVariableAndText(ref int variable, ref TMP_Text text, int varVal, int textVal) 
    {
        variable = varVal;
        text.SetText(textVal.ToString());
    }
    void Start()
    {
        Time.timeScale = 1.0f;
        instructionsWindow.SetActive(false);

        HowTo.onClick.AddListener(() =>
        {
            instructionsWindow.SetActive(true);
        });
        closeWindow.onClick.AddListener(() =>
        {
            instructionsWindow.SetActive(false);
        });
        Quit.onClick.AddListener(() =>
        {
            Application.Quit();
        });
        RenderSettings.skybox = skyboxMat;
        skyboxMat.SetFloat("_StarSpeed", starSpeed);
        useChosenSettings = true;
        renderDistanceSlider.wholeNumbers = true;
        renderDistanceSlider.minValue = 1;
        renderDistanceSlider.maxValue = 7;
        renderDistanceSlider.value = 2;
        SetVariableAndText(ref renderDistance, ref renderDistanceText, Mathf.RoundToInt(renderDistanceSlider.value * 180), (int) renderDistanceSlider.value);
        renderDistanceSlider.onValueChanged.AddListener((value) =>
        {
            SetVariableAndText(ref renderDistance, ref renderDistanceText, Mathf.RoundToInt(value * 180), (int)value);
        });
        starCountSlider.wholeNumbers = true;
        starCountSlider.minValue = 10;
        starCountSlider.value = 50;
        starCountSlider.maxValue = 300;
        SetVariableAndText(ref starCount, ref starCountText, Mathf.RoundToInt(starCountSlider.value * 100));
        starCountSlider.onValueChanged.AddListener((value) =>
        {
            SetVariableAndText(ref starCount, ref starCountText, Mathf.RoundToInt(value * 100));
        });
        planetResSlider.wholeNumbers = true;
        planetResSlider.minValue = 1;
        planetResSlider.maxValue = 24;
        planetResSlider.value = 8;
        SetVariableAndText(ref planetRes, ref planetResText, Mathf.RoundToInt(planetResSlider.value));
        planetResSlider.onValueChanged.AddListener((value) =>
        {
            SetVariableAndText(ref planetRes, ref planetResText, Mathf.RoundToInt(value));
        });
        starResSlider.wholeNumbers = true;
        starResSlider.minValue = 1;
        starResSlider.maxValue = 24;
        starResSlider.value = 8;
        SetVariableAndText(ref starRes, ref starResText, Mathf.RoundToInt(starResSlider.value));
        starResSlider.onValueChanged.AddListener((value) =>
        {
            SetVariableAndText(ref starRes, ref starResText, Mathf.RoundToInt(value));
        });


        ExploreButton.onClick.AddListener(() => { SceneManager.LoadScene(1);});

        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnDestroy()
    {
        skyboxMat.SetFloat("_StarSpeed", 0.0f);
    }
}
