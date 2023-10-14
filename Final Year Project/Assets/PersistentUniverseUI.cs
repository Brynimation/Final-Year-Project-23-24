using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Unity.VisualScripting;

public class PersistentUniverseUI : MonoBehaviour
{
    [SerializeField] TMP_Text coord;
    [SerializeField] GameManager manager;
    Vector3Int viewerCoord;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        viewerCoord = new Vector3Int(Mathf.RoundToInt(manager.viewer.position.x), Mathf.RoundToInt(manager.viewer.position.y), Mathf.RoundToInt(manager.viewer.position.z));
        coord.SetText(viewerCoord.ToString());
    }
}
