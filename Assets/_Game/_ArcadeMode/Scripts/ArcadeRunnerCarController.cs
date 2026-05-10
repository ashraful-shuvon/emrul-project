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

    [Header("Grip")]
    [Range(0f, 1f)]
    public float grip = 0.9f;

    [Header("Stability")]
    public float downforce = 20f;
    public float extraGravity = 30f;

    [Header("Steering Assist")]
    [Range(0f, 1f)]
    public float steeringAssist = 0.05f;

    [Header("Jumping")]
    public float jumpForce = 8f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    [HideInInspector] public float steerInput;
    private float throttleInput;
    private bool isGrounded;
    private bool jumpRequested;

    public bool IsGrounded => isGrounded;


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
        if (value.isPressed)
            jumpRequested = true;
    }

    // =========================
    // FIXED UPDATE
    // =========================

    void FixedUpdate()
    {
        CheckGrounded();
        ApplyMovement();
        ApplySteering();
        ApplyGrip();
        ApplyDownforce();
        ApplyJump();
    }

    // =========================
    // MOVEMENT
    // =========================

    void ApplyMovement()
    {
        float finalThrottle = autoAccelerate ? autoThrottle : throttleInput;

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

        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

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
    // STEERING
    // =========================

    void ApplySteering()
    {
        float speedFactor = rb.linearVelocity.magnitude / maxSpeed;
        float steerAmount = steerInput * turnSpeed * speedFactor;

        Quaternion turnRot = Quaternion.Euler(
            0f,
            steerAmount * Time.fixedDeltaTime,
            0f
        );

        rb.MoveRotation(rb.rotation * turnRot);
    }

    // =========================
    // GRIP
    // =========================

    void ApplyGrip()
    {
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        localVel.x *= (1f - grip);
        rb.linearVelocity = transform.TransformDirection(localVel);

        rb.linearVelocity = Vector3.Lerp(
            rb.linearVelocity,
            transform.forward * rb.linearVelocity.magnitude,
            steeringAssist
        );
    }

    // =========================
    // DOWNFORCE
    // =========================

    void ApplyDownforce()
    {
        if (!isGrounded) return;

        rb.AddForce(Vector3.down * downforce * rb.linearVelocity.magnitude);
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
    }
}