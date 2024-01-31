using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
/*
public class HUD : MonoBehaviour
{
    [SerializeField] Camera cam;
    [SerializeField] TMP_Text starText;
    [SerializeField] StarSystem selectedStarSystem;
    Ray ray;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 mousePos = Input.mousePosition;
        ray = cam.ScreenPointToRay(mousePos);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Star selectedStar = hit.transform.GetComponent<Star>();
            if (selectedStar != null)
            {
                starText.gameObject.SetActive(true);
                starText.text = "Star: " + selectedStar.starProperties.starId;
                starText.rectTransform.position = cam.WorldToScreenPoint(hit.point);
            }
        }
        else {
            starText.gameObject.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(ray);
    }
}
*/