using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniverseGenerator : MonoBehaviour
{
    [SerializeField] float sectorWidth;
    [SerializeField] float sectorHeight;
    int numSectorsX;
    int numSectorsY;
    Vector3 prevCamPos;
    [SerializeField] GameObject starPrefab;
    List<GameObject> instantiatedStars;
    Vector3Int initialCameraPos;
    Vector2 cameraOffset;
    private void Start()
    {
        initialCameraPos = new Vector3Int((int) Camera.main.transform.position.x, (int) Camera.main.transform.position.y, (int) Camera.main.nearClipPlane + +50);
        cameraOffset = Vector2.zero;
        prevCamPos = Camera.main.transform.position;
        instantiatedStars = new List<GameObject>();
        int numStarsX = 16;
        int numStarsY = 16;

        Vector2 sectorDimensions = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width/numStarsX, Screen.height/numStarsY, Camera.main.nearClipPlane + +50f));
        Debug.Log(sectorDimensions);

        for (int x = 0; x < numStarsX; x++) 
        {
            for (int y = 0; y < numStarsY; y++) 
            {
                Vector3 cameraWorldRotation = Camera.main.transform.rotation.eulerAngles;
                Vector3 centreWorldPos = Camera.main.ViewportToWorldPoint(new Vector2(0.5f, 0.5f));
                float xCentre = centreWorldPos.x;
                float yCentre = centreWorldPos.y;
                StarSystem starSystem = new StarSystem(x + xCentre, y + yCentre);
                if (starSystem.starExists)
                {
                    Vector3 pos = new Vector3(x * sectorDimensions.x + sectorDimensions.x / 2f, y * sectorDimensions.y + sectorDimensions.y / 2f, Camera.main.nearClipPlane + 50f); ;
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(pos);
                    GameObject star = Instantiate(starPrefab, worldPos, Quaternion.identity);
                    star.transform.localScale = Vector3.one * starSystem.starRadius;
                    star.GetComponent<Renderer>().material.color = starSystem.starColour;
                    instantiatedStars.Add(star);
                }

            }
        }
    }
    void start()
    {
        Vector3 minPos = Camera.main.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        Vector3 maxPos = Camera.main.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
        numSectorsX = 16; //Mathf.Abs((int) ((maxPos.x - minPos.x) / sectorWidth));
        numSectorsY = 16; //Mathf.Abs((int) ((maxPos.y - minPos.y) / sectorHeight));
        sectorWidth = (Mathf.Abs(maxPos.x - minPos.x)/numSectorsX);
        sectorHeight = (Mathf.Abs(maxPos.y - minPos.y)/numSectorsY);
        instantiatedStars = new List<GameObject>();


        for (int x = 0; x < numSectorsX; x++)
        {
            for (int y = 0; y < numSectorsY; y++)
            {
                Vector3 cameraWorldRotation = Camera.main.transform.rotation.eulerAngles;
                int xRot = (int)cameraWorldRotation.x;
                int yRot = (int)cameraWorldRotation.y;
                int zRot = (int)cameraWorldRotation.z;
                StarSystem starSystem = new StarSystem(x, y);
                if (starSystem.starExists)
                {
                    float centreX = x * sectorWidth + sectorWidth / 2f;
                    float centreY = y * sectorHeight + sectorHeight / 2f;
                    Vector3 pos = new Vector3(centreX, centreY, Camera.main.farClipPlane);
                    GameObject star = Instantiate(starPrefab, pos, Quaternion.identity);
                    instantiatedStars.Add(star);
                    star.transform.localScale = Vector3.one * starSystem.starRadius;
                    star.GetComponent<Renderer>().material.color = starSystem.starColour;
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        int numSectorsX = 16;
        int numSectorsY = 16;
        int sectorWidth = Camera.main.pixelWidth / numSectorsX;
        int sectorHeight = Camera.main.pixelHeight / numSectorsY;

        for (int x = 0; x < numSectorsX; x++) 
        {
            for (int y = 0; y < numSectorsY; y++) 
            {
                StarSystem starSystem = new StarSystem(x, y);
                if (starSystem.starExists) 
                {
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3((x * sectorWidth), (y * sectorHeight), Camera.main.nearClipPlane + 50f));
                    Gizmos.DrawWireSphere(worldPos, starSystem.starRadius);
                    //GameObject star = Instantiate(starPrefab, worldPos, Quaternion.identity);
                    //star.transform.localScalestarSystem.starRadius 
                }
            }
        }
    }

    private void StartRotation()
    {
        prevCamPos = Camera.main.ScreenToViewportPoint(Input.mousePosition);
    }
    private void RotateCamera() 
    {
        Vector3 dir = prevCamPos - Camera.main.ScreenToViewportPoint(Input.mousePosition); //direction to rotate the camera
        Camera.main.transform.Rotate(Vector3.right, dir.y * 180); //rotate the camera about its local x axis
        Camera.main.transform.Rotate(Vector3.up, -dir.x * 180, Space.World);//rotate the camera about the world y axis 
        prevCamPos = Camera.main.ScreenToViewportPoint(Input.mousePosition);
    }
    private void Update()
    {
        if (Input.GetKey(KeyCode.D)) 
        {
            cameraOffset.x += 25 * Time.fixedDeltaTime;
            foreach (GameObject go in instantiatedStars)
            {
                GameObject.Destroy(go);
            }
            Vector3 camPos = Camera.main.transform.position;
            camPos.x += 25 * Time.fixedDeltaTime;
            Camera.main.transform.position = camPos;
        }
        if (Input.GetKey(KeyCode.A)) 
        {
            cameraOffset.x -= 25 * Time.fixedDeltaTime;
            foreach (GameObject go in instantiatedStars)
            {
                GameObject.Destroy(go);
            }
            Vector3 camPos = Camera.main.transform.position;
            camPos.x -= 25 * Time.fixedDeltaTime;
            Camera.main.transform.position = camPos;
        }
        if (Input.GetKey(KeyCode.W)) 
        {
            cameraOffset.y += 25 * Time.fixedDeltaTime;
            foreach (GameObject go in instantiatedStars)
            {
                GameObject.Destroy(go);
            }
            Vector3 camPos = Camera.main.transform.position;
            camPos.y += 25 * Time.fixedDeltaTime;
            Camera.main.transform.position = camPos;
        }
        if (Input.GetKey(KeyCode.S)) 
        {
            cameraOffset.y -= 25 * Time.fixedDeltaTime;
            foreach (GameObject go in instantiatedStars)
            {
                GameObject.Destroy(go);
            }
            Vector3 camPos = Camera.main.transform.position;
            camPos.y -= 25 * Time.fixedDeltaTime;
            Camera.main.transform.position = camPos;
        }
        int numSectorsX = 16;
        int numSectorsY = 16;
        int sectorWidth = Camera.main.pixelWidth / numSectorsX;
        int sectorHeight = Camera.main.pixelHeight / numSectorsY;
        if (prevCamPos == Camera.main.transform.position) return;
        foreach (GameObject go in instantiatedStars)
        {
            GameObject.Destroy(go);
        }
        instantiatedStars = new List<GameObject>();
        for (int x = 0; x < numSectorsX; x++)
        {
            for (int y = 0; y < numSectorsY; y++)
            {
                Vector3 centreWorldPos = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, Camera.main.farClipPlane));
                float xCentre = centreWorldPos.x;
                float yCentre = centreWorldPos.y;
                StarSystem starSystem = new StarSystem(xCentre + x, yCentre + y);
                if (starSystem.starExists)
                {
                    Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3((x * sectorWidth), (y * sectorHeight), Camera.main.nearClipPlane + 50f));
                    GameObject star = Instantiate(starPrefab, worldPos, Quaternion.identity);
                    star.transform.localScale *= starSystem.starRadius;
                    star.GetComponent<Renderer>().material.color = starSystem.starColour;
                    instantiatedStars.Add(star); 
                }
            }
        }
        prevCamPos = Camera.main.transform.position; 
    }
    /*private void Update()
    {
        Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight, Camera.main.nearClipPlane + 50f));
        int numSectorsX = 16;
        int numSectorsY = 16;
        float sectorWidth = Mathf.Abs(topRight.x / numSectorsX);
        float sectorHeight = Mathf.Abs(topRight.y / numSectorsY);
        if (Camera.main.transform.position != prevCamPos) 
        {
            foreach (GameObject go in instantiatedStars)
            {
                GameObject.Destroy(go);
            }
            instantiatedStars = new List<GameObject>();
            int numStarsX = 16;
            int numStarsY = 16;

            Vector2 sectorDimensions = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width / numStarsX, Screen.height / numStarsY, Camera.main.nearClipPlane +50f));
            Debug.Log(sectorDimensions);

            for (int x = 0; x < numStarsX; x++)
            {
                for (int y = 0; y < numStarsY; y++)
                {
                    StarSystem starSystem = new StarSystem(x, y);
                    if (starSystem.starExists)
                    {
                        Vector3 pos = new Vector3(x * sectorDimensions.x + sectorDimensions.x / 2f, y * sectorDimensions.y + sectorDimensions.y / 2f, Camera.main.nearClipPlane + 50f); ;
                        Vector3 worldPos = Camera.main.ScreenToWorldPoint(pos);
                        GameObject star = Instantiate(starPrefab, worldPos, Quaternion.identity);
                        star.transform.localScale = Vector3.one * starSystem.starRadius;
                        star.GetComponent<Renderer>().material.color = starSystem.starColour;
                        instantiatedStars.Add(star);
                    }

                }
            }
        }
        prevCamPos = Camera.main.transform.position;


    }*/

}
