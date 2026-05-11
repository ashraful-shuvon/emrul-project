using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class ArcadeRunnerCarController : MonoBehaviour
{
    [Header("Auto Drive")]
    public bool autoAccelerate = true;
    public float autoThrottle = 1f;

    [Header("Movement")]
    public float acceleration = 30f;
    public float maxSpeed = 20f;
    public float brakeForce = 40f;

    [Header("Steering")]
    public float turnSpeed = 120f;
    public float airSteerForce = 10f;

    [Header("Grip")]
    [Range(0f, 1f)]
    public float grip = 0.9f;

    [Header("Stability")]
    public float downforce = 20f;
    public float extraGravity = 30f;

    [Header("Jumping")]
    public float jumpForce = 8f;
    public float wingJumpForce = 12f;
    public float wingForwardBoost = 15f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    public bool IsGrounded => isGrounded;
    [HideInInspector] public float steerInput;
    [HideInInspector] public bool isDoubleJumping;
    [HideInInspector] public bool wingsClosed;

    private Rigidbody rb;
    private float throttleInput;
    private bool isGrounded, jumpRequested, wingJumpRequested, wasGroundedLastFrame;
    private int jumpCount;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        if (groundLayer.value == 0) groundLayer = LayerMask.GetMask("Ground");
    }

    public void OnMove(InputValue value)
    {
        var input = value.Get<Vector2>();
        steerInput = input.x;
        throttleInput = input.y;
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        if (isGrounded)
        {
            jumpCount = 1; jumpRequested = true;
            isDoubleJumping = false; wingsClosed = false;
        }
        else if (jumpCount == 1)
        {
            jumpCount = 2; isDoubleJumping = true; wingJumpRequested = true;
        }
        else if (jumpCount == 2)
        {
            jumpCount = 3; isDoubleJumping = false; wingsClosed = true;
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.down * 10f, ForceMode.VelocityChange);
        }
    }

    void FixedUpdate()
    {
        CheckGrounded();
        ApplyMovement();
        ApplySteering();
        ApplyDownforce();
        ApplyJump();
        ApplyWingJump();
    }

    void CheckGrounded()
    {
        if (groundCheck == null) return;
        isGrounded = Physics.CheckSphere(
            groundCheck.position, groundCheckRadius,
            groundLayer, QueryTriggerInteraction.Ignore
        );
    }

    void ApplyMovement()
    {
        float throttle = autoAccelerate ? autoThrottle : throttleInput;
        float force = throttle > 0 ? acceleration : brakeForce;
        if (throttle != 0)
            rb.AddForce(transform.forward * throttle * force, ForceMode.Acceleration);

        Vector3 flat = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (flat.magnitude > maxSpeed)
        {
            Vector3 capped = flat.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(capped.x, rb.linearVelocity.y, capped.z);
        }
    }

    void ApplySteering()
    {
        float turnAmount = steerInput * turnSpeed * Time.fixedDeltaTime;

        if (isGrounded)
        {
            turnAmount *= rb.linearVelocity.magnitude / maxSpeed;
            Quaternion turnRot = Quaternion.Euler(0f, turnAmount, 0f);
            rb.MoveRotation(rb.rotation * turnRot);

            // Rotate flat velocity with car — keeps velocity aligned
            Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 rotated = turnRot * flat;
            rb.linearVelocity = new Vector3(rotated.x, rb.linearVelocity.y, rotated.z);

            // Grip runs after rotation — no fighting
            Vector3 local = transform.InverseTransformDirection(rb.linearVelocity);
            local.x = Mathf.Lerp(local.x, 0f, grip * Time.fixedDeltaTime * 60f);
            rb.linearVelocity = transform.TransformDirection(local);
        }
        else
        {
            rb.AddForce(transform.right * steerInput * airSteerForce, ForceMode.Acceleration);
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount * 0.4f, 0f));
        }
    }

    void ApplyDownforce()
    {
        if (!isGrounded) return;
        rb.AddForce(Vector3.down * downforce * rb.linearVelocity.magnitude);
        rb.AddForce(Vector3.down * extraGravity);
    }

    void ApplyJump()
    {
        if (jumpRequested && isGrounded)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        jumpRequested = false;

        if (isGrounded && !wasGroundedLastFrame)
        {
            jumpCount = 0; isDoubleJumping = false; wingsClosed = false;
        }

        wasGroundedLastFrame = isGrounded;
    }

    void ApplyWingJump()
    {
        if (!wingJumpRequested) return;
        wingJumpRequested = false;
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(Vector3.up * wingJumpForce, ForceMode.VelocityChange);
        rb.AddForce(transform.forward * wingForwardBoost, ForceMode.VelocityChange);
    }
}