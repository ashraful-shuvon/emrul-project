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

    [Header("Air Acceleration")]
    [Range(0f, 2f)]
    public float airAccelMultiplier = 1.2f;

    [Header("Steering")]
    public float turnSpeed = 200f;
    public float airSteerForce = 10f;

    [Header("Grip")]
    [Range(0f, 1f)]
    public float grip = 0.95f;

    [Header("Stability")]
    public float downforce = 20f;
    public float extraGravity = 15f;
    public float wingFoldGravity = 25f;

    [Header("Jumping & Soft Slam")]
    public float jumpForce = 11f;
    public float wingJumpForce = 30f;
    public float wingForwardBoost = 15f;
    public float smoothSlamTargetSpeed = -12f;

    [Header("Wing Fall Settings")]
    public float wingFallGravity = 20f;

    [Header("Ground Check & Stable Landing")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;
    public float groundAlignmentSpeed = 12f;
    public float landingGlueForce = 15f;

    [Header("Visual Weight Transfer")]
    public Transform carVisualModel;
    public float pitchAmount = 0f;
    public float rollAmount = 0f;
    public float tiltSpeed = 10f;

    public bool IsGrounded => isGrounded;
    [HideInInspector] public float steerInput;
    [HideInInspector] public bool isDoubleJumping;
    [HideInInspector] public bool wingsClosed;

    private Rigidbody rb;
    private float throttleInput;
    private bool isGrounded, jumpRequested, wingJumpRequested, wasGroundedLastFrame;
    private int jumpCount;
    private float currentGroundProximity = 1f;
    private bool wingFalling = false;

    private float groundedBuffer = 0f;
    private float groundedBufferTime = 0.1f; // stays grounded for 0.1s after losing contact


    // =========================
    // INIT
    // =========================

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = new Vector3(0, -0.5f, 0f);
        if (groundLayer.value == 0)
            groundLayer = LayerMask.GetMask("Ground");
    }

    // =========================
    // INPUT
    // =========================

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
            wingFalling = false;
        }
        else if (jumpCount == 1)
        {
            jumpCount = 2; isDoubleJumping = true;
            wingJumpRequested = true;
            wingFalling = false;
        }
        else if (jumpCount == 2)
        {
            // Third press — smooth fall
            jumpCount = 3;
            isDoubleJumping = false;
            wingsClosed = true;
            wingFalling = true;
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
        ApplyDownforce();
        ApplyJump();
        ApplyWingJump();
        AlignToGroundProfile();

        if (!isGrounded)
        {
            // Keep rotation flat in air
            Vector3 currentAngles = rb.rotation.eulerAngles;
            rb.MoveRotation(Quaternion.Euler(0f, currentAngles.y, 0f));

            // In FixedUpdate replace the Y lock block with this
            if (isDoubleJumping && !wingFalling)
            {
                // Only lock Y when car is falling — let it rise freely first
                if (rb.linearVelocity.y < 0)
                {
                    rb.linearVelocity = new Vector3(
                        rb.linearVelocity.x,
                        0f,
                        rb.linearVelocity.z
                    );
                }
            }
        }
    }

    void LateUpdate()
    {
        ApplyVisualWeightTransfer();
    }

    // =========================
    // GROUND CHECK
    // =========================

    void CheckGrounded()
    {
        if (groundCheck == null) return;

        bool sphereCheck = Physics.CheckSphere(
            groundCheck.position, groundCheckRadius,
            groundLayer, QueryTriggerInteraction.Ignore
        );

        if (sphereCheck)
        {
            groundedBuffer = groundedBufferTime;
            isGrounded = true;
        }
        else
        {
            groundedBuffer -= Time.fixedDeltaTime;
            isGrounded = groundedBuffer > 0f;
        }
    }

    // =========================
    // MOVEMENT
    // =========================

    void ApplyMovement()
    {
        float throttle = autoAccelerate ? autoThrottle : throttleInput;
        float force = throttle > 0 ? acceleration : brakeForce;

        if (isGrounded)
        {
            if (throttle != 0)
                rb.AddForce(
                    transform.forward * throttle * force,
                    ForceMode.Acceleration
                );
        }
        else
        {
            // Always accelerate in air — double jump flies forward freely
            if (throttle != 0)
                rb.AddForce(
                    transform.forward * throttle * force * airAccelMultiplier,
                    ForceMode.Acceleration
                );
        }

        Vector3 flatVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (flatVelocity.magnitude > maxSpeed)
        {
            Vector3 cappedFlat = flatVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(cappedFlat.x, rb.linearVelocity.y, cappedFlat.z);
        }
    }

    // =========================
    // STEERING
    // =========================

    void ApplySteering()
    {
        float turnAmount = steerInput * turnSpeed * Time.fixedDeltaTime;

        if (isGrounded)
        {
            turnAmount *= rb.linearVelocity.magnitude / maxSpeed;
            Quaternion turnRot = Quaternion.Euler(0f, turnAmount, 0f);
            rb.MoveRotation(rb.rotation * turnRot);

            Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 rotated = turnRot * flat;
            rb.linearVelocity = new Vector3(rotated.x, rb.linearVelocity.y, rotated.z);

            Vector3 local = transform.InverseTransformDirection(rb.linearVelocity);
            local.x = Mathf.Lerp(local.x, 0f, grip * Time.fixedDeltaTime * 120f);
            rb.linearVelocity = transform.TransformDirection(local);
        }
        else
        {
            rb.AddForce(
                transform.right * steerInput * airSteerForce,
                ForceMode.Acceleration
            );
            rb.MoveRotation(
                rb.rotation * Quaternion.Euler(0f, turnAmount * 0.5f, 0f)
            );

            // Steer while double jumping = start falling
            if (isDoubleJumping && Mathf.Abs(steerInput) > 0.1f && !wingFalling)
                wingFalling = true;
        }
    }

    // =========================
    // DOWNFORCE
    // =========================

    void ApplyDownforce()
    {
        if (isGrounded)
        {
            // Only downforce — no extra gravity on ground
            // Extra gravity was causing bouncing between hex tiles
            rb.AddForce(Vector3.down * downforce * rb.linearVelocity.magnitude);
            return;
        }

        // Air only
        if (isDoubleJumping && !wingFalling)
            return;

        if (wingFalling)
        {
            if (rb.linearVelocity.y > 0)
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    Mathf.MoveTowards(rb.linearVelocity.y, 0f, wingFoldGravity * Time.fixedDeltaTime),
                    rb.linearVelocity.z
                );
            }

            float targetY = Mathf.MoveTowards(
                rb.linearVelocity.y,
                smoothSlamTargetSpeed,
                Time.fixedDeltaTime * wingFallGravity
            );

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, targetY, rb.linearVelocity.z);
        }
        else
        {
            float dynamicGravity = Mathf.Lerp(0.2f, 1f, currentGroundProximity);
            rb.AddForce(Vector3.down * extraGravity * dynamicGravity, ForceMode.Acceleration);
        }
    }

    // =========================
    // JUMP
    // =========================

    void ApplyJump()
    {
        if (jumpRequested && isGrounded)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

        jumpRequested = false;

        if (isGrounded && !wasGroundedLastFrame)
        {
            jumpCount = 0;
            isDoubleJumping = false;
            wingsClosed = false;
            wingFalling = false;

            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckRadius + 2f, groundLayer))
            {
                Vector3 groundNormal = hit.normal;
                Quaternion targetRot = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
                rb.MoveRotation(targetRot);
                rb.AddForce(-groundNormal * landingGlueForce, ForceMode.VelocityChange);
            }
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

        rb.AddForce(Vector3.up * wingJumpForce, ForceMode.VelocityChange);
        rb.AddForce(transform.forward * wingForwardBoost, ForceMode.VelocityChange);
    }

    // =========================
    // GROUND ALIGNMENT
    // =========================

    void AlignToGroundProfile()
    {
        if (!isGrounded) return;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, groundCheckRadius + 1.5f, groundLayer))
        {
            Vector3 groundNormal = hit.normal;
            Quaternion targetRot = Quaternion.FromToRotation(transform.up, groundNormal) * transform.rotation;
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, Time.fixedDeltaTime * groundAlignmentSpeed));
        }
    }

    // =========================
    // VISUAL WEIGHT TRANSFER
    // =========================

    void ApplyVisualWeightTransfer()
    {
        if (carVisualModel == null) return;

        float targetPitch = (autoAccelerate ? autoThrottle : throttleInput) * -pitchAmount;
        float targetRoll = steerInput * -rollAmount;

        if (throttleInput < 0) targetPitch = pitchAmount * 1.5f;

        Quaternion targetRotation = Quaternion.Euler(targetPitch, 0, targetRoll);
        carVisualModel.localRotation = Quaternion.Slerp(
            carVisualModel.localRotation,
            targetRotation,
            Time.deltaTime * tiltSpeed
        );
    }
}