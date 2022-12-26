using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class Body : MonoBehaviour
{
    public static List<Body> bodies;
    public static float G = 1f;
    public Rigidbody rb;

    private void OnEnable()
    {
        if (bodies == null) 
        {
            bodies = new List<Body>();
        }
        bodies.Add(this);
    }
    private void OnDisable()
    {
        bodies.Remove(this);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.detectCollisions = false;
        
    }

    void Attract(Body otherBody) 
    {
        Vector3 displacement = rb.position - otherBody.rb.position;
        if(displacement.sqrMagnitude > 0.1f) {
            Vector3 forceDir = displacement.normalized;
            float forceMag = G * rb.mass * otherBody.rb.mass / displacement.sqrMagnitude;
            Debug.Log(forceDir * forceMag);
            otherBody.rb.AddForce(forceMag * forceDir);
        }

    }

    // Update is called once per frame
    void Update()
    {
        foreach (Body b in bodies) 
        {
            if (b != this) 
            {
                Attract(b);
            }
        }
    }
}
