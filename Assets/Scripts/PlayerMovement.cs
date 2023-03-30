using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    KeyCode jump = KeyCode.Space;
    [Header("Movement")]
    public float movementSpeed;
    public float terminalVelocity;
    public float groundDrag;
    public float jumpForce, jumpCooldown, airMultiplier;
    bool readyToJump;
    [Header("Ground Check")]
    public float playerHeight;
    public LayerMask ground;
    public bool onGround;
    public Transform orientation;
    float horizontalInput, verticalInput;
    Vector3 movementDirection;
    Rigidbody rigidBody;

    // Start is called before the first frame update
    void Start()
    {
        rigidBody = this.GetComponent<Rigidbody>();
        rigidBody.freezeRotation = true;
        readyToJump = true;
    }

    // Update is called once per frame
    void Update()
    {
        onGround = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.2f, ground);
        PlayerInput();
        if(onGround)
            rigidBody.drag = groundDrag;
        else
            rigidBody.drag = 0;

        MaxSpeed();
    }

    private void FixedUpdate() 
    {
        MovePlayer();
    }

    private void PlayerInput() 
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if(Input.GetKey(jump) && onGround && readyToJump)
        {
            readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
    }

    private void MovePlayer() 
    {
        movementDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
        if(onGround)
        {
            rigidBody.AddForce(movementDirection.normalized * movementSpeed * 10f, ForceMode.Force);
        }else if(!onGround)
        {
            rigidBody.AddForce(movementDirection.normalized * movementSpeed * 10f * airMultiplier, ForceMode.Force);
        }
    }

    private void MaxSpeed() 
    {
        Vector3 flatVelocity = new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);

        if(flatVelocity.magnitude > movementSpeed && onGround) 
        {
            Vector3 limitVelocity = flatVelocity.normalized * movementSpeed;
            rigidBody.velocity =new Vector3(limitVelocity.x, rigidBody.velocity.y, limitVelocity.z);
        } else if(flatVelocity.magnitude > terminalVelocity && !onGround)
        {
            Vector3 limitVelocity = flatVelocity.normalized * terminalVelocity;
            rigidBody.velocity = new Vector3(limitVelocity.x, rigidBody.velocity.y, limitVelocity.z);
        }
    }  

    public void Jump() 
    {
        rigidBody.velocity = new Vector3(rigidBody.velocity.x, 0f, rigidBody.velocity.z);
        rigidBody.AddForce(transform.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump() 
    {
        readyToJump = true;
    }  
}
