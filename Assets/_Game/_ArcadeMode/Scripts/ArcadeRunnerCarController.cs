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

    [Header("Air Steering")]
    public float airSteerForce = 10f;

    [Header("Grip")]
    [Range(0f, 1f)]
    public float grip = 0.9f;

    [Header("Stability")]
    public float downforce = 20f;
    public float extraGravity = 30f;

    [Header("Jumping")]
    public float jumpForce = 8f;

    [Header("Double Jump / Wings")]
    public float wingJumpForce = 12f;
    public float wingForwardBoost = 15f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    // Public accessors
    public bool IsGrounded => isGrounded;
    [HideInInspector] public float steerInput;
    [HideInInspector] public bool isDoubleJumping = false;
    [HideInInspector] public bool wingsClosed = false;

    private Rigidbody rb;
    private float throttleInput;
    private bool isGrounded;
    private bool jumpRequested;
    private bool wingJumpRequested;
    private int jumpCount = 0;
    private bool wasGroundedLastFrame = true;

    // =========================
    // INIT
    // =========================

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");
    }

    // =========================
    // INPUT
    // =========================

    public void OnMove(InputValue value)
    {
        Vector2 input = value.Get<Vector2>();
        steerInput = input.x;
        throttleInput = input.y;
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        if (isGrounded)
        {
            jumpCount = 1;
            jumpRequested = true;
            isDoubleJumping = false;
            wingsClosed = false;
        }
        else if (jumpCount == 1)
        {
            jumpCount = 2;
            isDoubleJumping = true;
            wingJumpRequested = true;
        }
        else if (jumpCount == 2)
        {
            jumpCount = 3;
            isDoubleJumping = false;
            wingsClosed = true;

            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );

            rb.AddForce(
                Vector3.down * 10f,
                ForceMode.VelocityChange
            );
        }
    }

    // =========================
    // FIXED UPDATE
    // =========================

    void FixedUpdate()
    {
        CheckGrounded();
        ApplyMovement();
        ApplySteering();
        ApplyAirSteering();
        ApplyGrip();
        ApplyDownforce();
        ApplyJump();
        ApplyWingJump();
    }

    // =========================
    // MOVEMENT
    // =========================

    void ApplyMovement()
    {
        float finalThrottle =
            autoAccelerate ? autoThrottle : throttleInput;

        if (finalThrottle > 0)
        {
            rb.AddForce(
                transform.forward * finalThrottle * acceleration,
                ForceMode.Acceleration
            );
        }
        else if (finalThrottle < 0)
        {
            rb.AddForce(
                transform.forward * finalThrottle * brakeForce,
                ForceMode.Acceleration
            );
        }

        // Speed limit
        Vector3 flatVel =
            new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(
                limitedVel.x,
                rb.linearVelocity.y,
                limitedVel.z
            );
        }
    }

    // =========================
    // STEERING (GROUND)
    // =========================

    void ApplySteering()
    {
        if (!isGrounded) return;

        float speedFactor = rb.linearVelocity.magnitude / maxSpeed;

        float steerAmount =
            steerInput *
            turnSpeed *
            speedFactor *
            Time.fixedDeltaTime;

        Quaternion turnRot = Quaternion.Euler(0f, steerAmount, 0f);

        rb.MoveRotation(rb.rotation * turnRot);

        // Rotate only flat velocity — never touch Y
        Vector3 flatVel =
            new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        Vector3 rotatedFlat = turnRot * flatVel;

        rb.linearVelocity = new Vector3(
            rotatedFlat.x,
            rb.linearVelocity.y,
            rotatedFlat.z
        );
    }

    // =========================
    // STEERING (AIR)
    // =========================

    void ApplyAirSteering()
    {
        if (isGrounded) return;

        // Lateral force for drift feel in air
        rb.AddForce(
            transform.right *
            steerInput *
            airSteerForce,
            ForceMode.Acceleration
        );

        // Gentle rotation in air
        float airTurnAmount =
            steerInput *
            (turnSpeed * 0.4f) *
            Time.fixedDeltaTime;

        rb.MoveRotation(
            rb.rotation *
            Quaternion.Euler(0f, airTurnAmount, 0f)
        );
    }

    // =========================
    // GRIP
    // =========================

    void ApplyGrip()
    {
        if (!isGrounded) return;

        Vector3 localVel =
            transform.InverseTransformDirection(rb.linearVelocity);

        localVel.x = Mathf.Lerp(
            localVel.x,
            0f,
            grip * Time.fixedDeltaTime * 60f
        );

        rb.linearVelocity =
            transform.TransformDirection(localVel);
    }

    // =========================
    // DOWNFORCE
    // =========================

    void ApplyDownforce()
    {
        if (!isGrounded) return;

        rb.AddForce(
            Vector3.down * downforce * rb.linearVelocity.magnitude
        );

        rb.AddForce(Vector3.down * extraGravity);
    }

    // =========================
    // GROUND CHECK
    // =========================

    void CheckGrounded()
    {
        if (groundCheck == null) return;

        isGrounded = Physics.CheckSphere(
            groundCheck.position,
            groundCheckRadius,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    // =========================
    // JUMP
    // =========================

    void ApplyJump()
    {
        if (jumpRequested && isGrounded)
        {
            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.VelocityChange
            );
            jumpRequested = false;
        }
        else if (!isGrounded)
        {
            jumpRequested = false;
        }

        if (isGrounded && !wasGroundedLastFrame)
        {
            jumpCount = 0;
            isDoubleJumping = false;
            wingsClosed = false;
        }

        wasGroundedLastFrame = isGrounded;
    }

    // =========================
    // WING JUMP
    // =========================

    void ApplyWingJump()
    {
        if (!wingJumpRequested) return;

        wingJumpRequested = false;

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        rb.AddForce(
            Vector3.up * wingJumpForce,
            ForceMode.VelocityChange
        );

        rb.AddForce(
            transform.forward * wingForwardBoost,
            ForceMode.VelocityChange
        );
    }
}