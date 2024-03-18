using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OverlayUI : MonoBehaviour
{
    [SerializeField] Button menuButton;
    [SerializeField] Button quitButton;
    [SerializeField] Button resumeButton;

    private void OnEnable()
    {
        Time.timeScale = 0f;
    }
    void Start()
    {
        menuButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(0);
        });
        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });
        resumeButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1.0f;
            gameObject.SetActive(false);
        });
    }
    private void OnDisable()
    {
        Time.timeScale = 1.0f;
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
