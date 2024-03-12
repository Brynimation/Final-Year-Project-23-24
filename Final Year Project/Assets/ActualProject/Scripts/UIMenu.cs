using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class UIMenu : MonoBehaviour
{
    [SerializeField] Slider renderDistanceSlider;
    [SerializeField] TMP_Text renderDistanceText;

    [SerializeField] Slider starCountSlider;
    [SerializeField] TMP_Text starCountText;

    [SerializeField] Slider planetResSlider;
    [SerializeField] TMP_Text planetResText;

    [SerializeField] Slider starResSlider;
    [SerializeField] TMP_Text starResText;

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
    void Start()
    {
        useChosenSettings = true;
        renderDistanceSlider.wholeNumbers = true;
        renderDistanceSlider.minValue = 1;
        renderDistanceSlider.maxValue = 7;
        renderDistanceSlider.value = 2;
        SetVariableAndText(ref renderDistance, ref renderDistanceText, Mathf.RoundToInt(renderDistanceSlider.value * 180));
        renderDistanceSlider.onValueChanged.AddListener((value) =>
        {
            SetVariableAndText(ref renderDistance, ref renderDistanceText, Mathf.RoundToInt(value * 180));
            Debug.Log(renderDistance);
        });
        starCountSlider.wholeNumbers = true;
        starCountSlider.minValue = 10;
        starCountSlider.value = 100;
        starCountSlider.maxValue = 1000;
        SetVariableAndText(ref starCount, ref starCountText, Mathf.RoundToInt(starCountSlider.value * 100));
        starCountSlider.onValueChanged.AddListener((value) =>
        {
            SetVariableAndText(ref starCount, ref starCountText, Mathf.RoundToInt(value * 100));
        });
        planetResSlider.wholeNumbers = true;
        planetResSlider.value = 8;
        planetResSlider.minValue = 1;
        planetResSlider.maxValue = 24;
        SetVariableAndText(ref planetRes, ref planetResText, Mathf.RoundToInt(planetResSlider.value));
        planetResSlider.onValueChanged.AddListener((value) =>
        {
            SetVariableAndText(ref planetRes, ref planetResText, Mathf.RoundToInt(value));
        });
        starResSlider.wholeNumbers = true;
        starResSlider.value = 8;
        starResSlider.minValue = 1;
        starResSlider.maxValue = 24;
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
}
