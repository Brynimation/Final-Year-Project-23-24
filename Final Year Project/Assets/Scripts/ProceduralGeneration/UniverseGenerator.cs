using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniverseGenerator : MonoBehaviour
{
    [SerializeField] int sectorWidth;
    [SerializeField] int sectorHeight;
    int numSectorsX;
    int numSectorsY;
    [SerializeField] GameObject starPrefab;
    List<GameObject> instantiatedStars;
    void Start()
    {
        Vector3 minPos = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, -10f));
        Vector3 maxPos = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, -10f));
        numSectorsX = Mathf.Abs((int) ((maxPos.x - minPos.x) / sectorWidth));
        numSectorsY = Mathf.Abs((int) ((maxPos.y - minPos.y) / sectorHeight));
        Debug.Log(numSectorsX + ", " + numSectorsY);
        instantiatedStars = new List<GameObject>();


        for (int x = 0; x < numSectorsX; x++)
        {
            for (int y = 0; y < numSectorsY; y++)
            {
                StarSystem starSystem = new StarSystem(x, y);
                if (starSystem.starExists)
                {
                    int centreX = x * sectorWidth + sectorWidth / 2;
                    int centreY = y * sectorHeight + sectorHeight / 2;
                    Vector3 pos = new Vector3(centreX, centreY, Camera.main.farClipPlane);
                    GameObject star = Instantiate(starPrefab, pos, Quaternion.identity);
                    instantiatedStars.Add(star);
                    star.transform.localScale = Vector3.one * starSystem.starRadius;
                    star.GetComponent<Renderer>().material.color = starSystem.starColour;
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        
    }
}
