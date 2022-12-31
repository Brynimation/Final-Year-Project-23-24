using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [SerializeField] float mouseXSensitivity;
    [SerializeField] float mouseYSensitivity;
    [SerializeField] Vector2 minMaxVerticalLookRotation;
    float verticalLookRotation;
    Transform camT;
    void Start()
    {
        camT = Camera.main.transform;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * Time.deltaTime * mouseXSensitivity); //Rotate the player about the world's y axis
        verticalLookRotation += Input.GetAxis("Mouse Y") * mouseYSensitivity * Time.deltaTime;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, minMaxVerticalLookRotation.x, minMaxVerticalLookRotation.y);
        camT.localEulerAngles = Vector3.left * verticalLookRotation;
    }
}
