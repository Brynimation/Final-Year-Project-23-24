using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GalaxyProperties2 
{
    public Vector3 galacticCentre;
    public float minEccentricity;
    public float maxEccentricity;

    public float galacticDiskRadius;
    public float galacticBulgeRadius;
    public float angularOffsetMultiplier; //Determines number of spirals
    public int starCount;
    public Color standardColour;
    public Color H2RegionColour;

    private uint nLehmerSeed = 0;
    public GalaxyProperties2(Vector3 position, Vector2Int minMaxDisc, Vector2Int minMaxBulge, Vector2Int minMaxAngularOffset, Vector2Int minMaxStarCount) 
    {
        nLehmerSeed = (uint)(((int)position.x & 0xffff) << 16 | ((int)position.y & 0xffff));
        nLehmerSeed = (uint)(nLehmerSeed | (int)position.z & 0xffff);
        galacticDiskRadius = randInt(minMaxDisc.x, minMaxDisc.y);
        galacticBulgeRadius = randInt(minMaxBulge.x, minMaxBulge.y);
        angularOffsetMultiplier = randInt(minMaxAngularOffset.x, minMaxAngularOffset.y);
        starCount = randInt(minMaxStarCount.x, minMaxStarCount.y);
        minEccentricity = Random.Range(0.25f, 0.75f);
        maxEccentricity = Random.Range(minEccentricity, 1f);
        
    }

    /*
     * https://en.wikipedia.org/wiki/Lehmer_random_number_generator
     * https://www.youtube.com/watch?v=ZZY9YE7rZJw
    */
    private uint Lehmer32()
    {
        nLehmerSeed += 0xe120fc15;
        long tmp = (long)nLehmerSeed * 0x4a39b70d;
        long m1 = (tmp >> 32) ^ tmp;
        tmp = m1 * 0x12fad5c9;
        long m2 = (tmp >> 32) ^ tmp;
        return (uint)m2;
    }
    private int randInt(int min, int max)
    {
        return (int)(Lehmer32() % (max - min)) + min;
    }
    private float randFloat(float min, float max)
    {
        return ((float)Lehmer32() / (float)(0x7ffffff)) * (max - min) + min;
    }
}
public class Galaxy : MonoBehaviour
{
    AsyncOperation aSyncLoader;
    public Vector2Int minMaxDisc;
    public Vector2Int minMaxBulge;
    public Vector2Int minMaxAngularOffset;
    public Vector2Int minMaxStarCount;
    private void OnTriggerEnter(Collider other)
    {
        PlayerController player = other.gameObject.GetComponent<PlayerController>();
        if (player != null) 
        {
            UniGenerator.currentGalaxyProperties = new GalaxyProperties2(transform.position, minMaxDisc, minMaxBulge, minMaxAngularOffset, minMaxStarCount);
            StartCoroutine(LoadNextScene());
            
        }
    }

    IEnumerator LoadNextScene() 
    {
        aSyncLoader = SceneManager.LoadSceneAsync(1);
        aSyncLoader.allowSceneActivation = false;
        yield return aSyncLoader;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (aSyncLoader != null) 
        {
            if (aSyncLoader.progress >= 0.9f) 
            {
                aSyncLoader.allowSceneActivation = true;
            }
        }
    }
}
