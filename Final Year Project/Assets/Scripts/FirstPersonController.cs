using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [SerializeField] float mouseXSensitivity;
    [SerializeField] float mouseYSensitivity;
    [SerializeField] Vector2 minMaxVerticalLookRotation;
    [SerializeField] float walkSpeed;
    [SerializeField] float runSpeed;
    [SerializeField] float smoothTime;
    Rigidbody rb;
    float verticalLookRotation;
    Vector3 moveAmount;
    Vector3 currentVel;
    Transform camT;
    void Start()
    {
        camT = Camera.main.transform;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(Vector3.up * Input.GetAxis("Mouse X") * Time.deltaTime * mouseXSensitivity); //Rotate the player about the world's y axis
        verticalLookRotation += Input.GetAxis("Mouse Y") * mouseYSensitivity * Time.deltaTime;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, minMaxVerticalLookRotation.x, minMaxVerticalLookRotation.y);
        camT.localEulerAngles = Vector3.left * verticalLookRotation;

        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        moveDir.Normalize();
        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 targetMoveAmount = moveDir * moveSpeed;
        moveAmount = Vector3.SmoothDamp(moveAmount, targetMoveAmount, ref currentVel, smoothTime);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }
}
