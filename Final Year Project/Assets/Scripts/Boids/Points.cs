using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using static UnityEditor.PlayerSettings;

public class Points : MonoBehaviour
{
    [SerializeField] float turnFraction = 1.618f; 
    [SerializeField] int numPoints;
    [Range(0, 1)]
    [SerializeField] float pointRadius;
    [SerializeField] float scaleLength;
    [SerializeField] float maxRadius;
    [SerializeField] float I0;
    [SerializeField] Body bodyPrefab;
    Vector3[] points;
    public void GenerateDisk() 
    {
        //To get a good distribution of points, we want to avoid any points lining up.
        //To do this, we'll use an irrational number known as the golden ratio.
        points = new Vector3[(int)numPoints];
        for (int i = 0; i < numPoints; i++) 
        {
            float dist = scaleLength * Mathf.Log(i / (numPoints - 1f), Mathf.Exp(1)) - Mathf.Log(I0, Mathf.Exp(1));
            //Surface brightness distribution = I0 * e^R/scaleLength - exponential falloff with radius.
            //
            //float dist = I0 * Mathf.Exp(-(i/(numPoints - 1f)/scaleLength));
            //float dist = maxRadius * Mathf.Exp((-(float)i/(numPoints)) / scaleLength);
            float angle = Mathf.PI * 2 * i * turnFraction;
            float x = dist * Mathf.Cos(angle);
            float y = dist * Mathf.Sin(angle);
            points[i] = new Vector3(x, y, 0);

        }
    }

    private void Awake()
    {
        GenerateDisk();
        foreach (Vector3 point in points)
        {
            Instantiate(bodyPrefab, point, Quaternion.identity);
        }
    }
    private void OnDrawGizmos()
    {
        if (points != null)
        {
            foreach (Vector3 point in points){
                Gizmos.DrawWireSphere(point, pointRadius);
            }
        }
    }
    void Start()
    {
        GenerateDisk();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
