using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

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

    [Header("Juice - Visual Reference")]
    public Transform carVisuals; // drag your CarVisuals child here
    public float bodyTiltAngle = 8f;
    public float bodyTiltSpeed = 6f;

    private bool wasGroundedPrev = true;

    // Add this field near the top with other private fields:
    private CarFlip carFlip;

    [Header("Boost")]
    public float boostMultiplier = 2f;
    public float boostDuration = 3f;
    public float boostCooldown = 5f;

    private bool isBoosting = false;
    private float boostTimer = 0f;
    private float cooldownTimer = 0f;
    public bool IsBoosting => isBoosting;
    public float BoostProgress => Mathf.Clamp01(boostTimer / boostDuration); // 1 = full, 0 = done



    // =========================
    // INIT
    // =========================

    void Awake()
    {
        carFlip = GetComponent<CarFlip>();
        
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

    public void OnBoost(InputValue value)
    {
        if (value.isPressed)
            TryActivateBoost();
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

        if (!GetComponent<CarFlip>()?.enabled ?? false) // only if no flip active
            ApplyBodyTilt();

        
        // Landing squish
        GetComponent<WheelVisuals>()?.PlayLandingWobble();
        if (isGrounded && !wasGroundedPrev && carVisuals != null)
        {
            carVisuals.DOKill();
            carVisuals.DOScaleY(0.65f, 0.07f).SetEase(Ease.OutQuad)
                .OnComplete(() =>
                    carVisuals.DOScaleY(1.1f, 0.12f).SetEase(Ease.OutBack)
                        .OnComplete(() =>
                            carVisuals.DOScaleY(1f, 0.1f).SetEase(Ease.InOutSine)));
        }
        wasGroundedPrev = isGrounded;

        if (isBoosting)
        {
            boostTimer -= Time.fixedDeltaTime;
            if (boostTimer <= 0f)
            {
                isBoosting = false;
                cooldownTimer = boostCooldown;
            }
        }
        else if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.fixedDeltaTime;
        }
    }

    
    void Update()
    {
        
        if (carFlip == null || !carFlip.IsFlipping)
            ApplyBodyTilt();
    }

    // =========================
    // MOVEMENT
    // =========================

    void ApplyMovement()
    {
        float finalThrottle = autoAccelerate ? autoThrottle : throttleInput;

        /*if (finalThrottle > 0)
        {
            rb.AddForce(
                transform.forward * finalThrottle * acceleration,
                ForceMode.Acceleration
            );
        }*/

        if (finalThrottle > 0)
        {
            float boostMult = isBoosting ? boostMultiplier : 1f;
            rb.AddForce(
                transform.forward * finalThrottle * acceleration * boostMult,
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

        /*if (flatVel.magnitude > maxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(
                limitedVel.x,
                rb.linearVelocity.y,
                limitedVel.z
            );
        }*/
        float currentMaxSpeed = isBoosting ? maxSpeed * boostMultiplier : maxSpeed;
        if (flatVel.magnitude > currentMaxSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * currentMaxSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
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

    /*void ApplyJump()
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
    }*/

    void ApplyJump()
    {
        if (jumpRequested && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
            jumpRequested = false;

            // Squash on launch: squish down then spring up
            if (carVisuals != null)
            {
                carVisuals.DOKill();
                carVisuals.DOScaleY(0.6f, 0.08f).SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                        carVisuals.DOScaleY(1.15f, 0.15f).SetEase(Ease.OutBack)
                            .OnComplete(() =>
                                carVisuals.DOScaleY(1f, 0.1f).SetEase(Ease.InOutSine)));
            }
        }
        else if (!isGrounded)
        {
            jumpRequested = false;
        }
    }

    /*void ApplyBodyTilt()
    {
        if (carVisuals == null) return;

        float targetZ = -steerInput * bodyTiltAngle; // lean into turn
        Vector3 current = carVisuals.localEulerAngles;
        float currentZ = current.z > 180f ? current.z - 360f : current.z;
        float newZ = Mathf.LerpAngle(currentZ, targetZ, Time.fixedDeltaTime * bodyTiltSpeed);
        carVisuals.localEulerAngles = new Vector3(current.x, current.y, newZ);
    }*/

    void ApplyBodyTilt()
    {
        if (carVisuals == null) return;

        float targetZ = -steerInput * bodyTiltAngle;
        Vector3 current = carVisuals.localEulerAngles;
        float currentZ = current.z > 180f ? current.z - 360f : current.z;
        float newZ = Mathf.LerpAngle(currentZ, targetZ, Time.deltaTime * bodyTiltSpeed);
        carVisuals.localEulerAngles = new Vector3(current.x, current.y, newZ);
    }

    void TryActivateBoost()
    {
        if (isBoosting || cooldownTimer > 0f) return;
        isBoosting = true;
        boostTimer = boostDuration;
    }


}