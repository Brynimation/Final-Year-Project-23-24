using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//https://www.youtube.com/watch?v=_QajrabyTJc

public class PlayerController : MonoBehaviour
{
    [Header("Rotation Parameters")]
    [SerializeField] float mouseXSensitivity = 100f;
    [SerializeField] float mouseYSensitivity = 100f;
    [SerializeField] Vector2 minMaxRotation = new Vector2(-60f, 60f);

    [Header("Transforms")]
    [SerializeField] Transform playerBody;
    [SerializeField] Transform cameraTransform;

    [Header("Movement")]
    [SerializeField] Rigidbody rb;
    [SerializeField] float movementSmoothTime = 0.15f;
    [SerializeField] float flySpeed;
    [SerializeField] float fastFlySpeed;
    SpiralGalaxyGenerator dispatcher;
    Vector3 currentVelocity;
    Vector3 moveAmount;

    float xRotation = 0f;
    float yRotation = 0f;
    bool startMoving = false;
    bool singleGalaxy = false;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0)) 
        {
            startMoving = true;
        }
        if (!startMoving) return;
        //Gather mouse inputs
        float mouseX = Input.GetAxis("Mouse X") * mouseXSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseYSensitivity * Time.deltaTime;

        //When we move our mouse on the y axis, we rotate our CAMERA about the x axis
        xRotation -= mouseY;
        yRotation += mouseX;
        xRotation = Mathf.Clamp(xRotation, minMaxRotation.x, minMaxRotation.y);
        playerBody.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);

        //When we move our mouse on the x axis, we rotate our ENTIRE PLAYER about the Y axis.
        //playerBody.Rotate(Vector3.up * mouseX);
        float verticalMovement = (Input.GetKey(KeyCode.Space)) ? 1 : ((Input.GetKey(KeyCode.LeftControl) ? -1 : 0));
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), verticalMovement, Input.GetAxisRaw("Vertical")).normalized;
        Vector3 targetMoveAmount = (Input.GetKey(KeyCode.LeftShift) ? moveDir * fastFlySpeed : moveDir * flySpeed);
        moveAmount = Vector3.SmoothDamp(moveAmount, targetMoveAmount, ref currentVelocity, movementSmoothTime);
    
    }

    private void FixedUpdate()
    {
        //transform.TransformDirection converts moveAmount from world space to local space.
        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }
}
