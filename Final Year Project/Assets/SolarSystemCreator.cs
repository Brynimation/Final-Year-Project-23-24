using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SolarSystemCreator : MonoBehaviour
{

    public struct SolarSystem
    {
        public Vector3 starPosition;
        public float starRadius;
        public SolarSystem(Vector3 pos, float rad) 
        {
            starPosition = pos;
            starRadius = rad;
        }
    
    }
    public Transform player;
    public Mesh starMesh;
    public GameObject star;
    public int numStars;
    public float startFadeInDist;
    public float startFadeOutDist;
    public float fadeDist;
    public ComputeBuffer solarSystemBuffer;
    public ComputeBuffer solarSystemArgsBuffer;
    public SolarSystem[] solarSystems;
    public Material starMaterial;
    public Bounds bounds;

    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        starMaterial.SetFloat("_StartFadeInDist", startFadeInDist);
        starMaterial.SetFloat("_StartFadeOutDist", startFadeOutDist);
        starMaterial.SetFloat("_FadeDist", fadeDist);
        starMaterial.SetVector("playerPosition", player.position);
        starMaterial.SetVector("centre", star.transform.position);
    }
}
