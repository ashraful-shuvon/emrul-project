using UnityEngine;
using UnityEngine.InputSystem;

public class CarFlip : MonoBehaviour
{
    [Header("References")]
    public Transform carVisuals;

    [Header("Flip Settings")]
    public float flipSpeed = 720f;
    public float flipSmoothing = 15f; // Helps smooth out the rotation if you land mid-flip

    [Header("Air Lean (Z)")]
    public float maxAirLean = 25f;
    public float airLeanSpeed = 5f;
    public float airLeanReturnSpeed = 4f;

    [Header("Drift Yaw (Y)")]
    public float maxDriftYaw = 15f;
    public float driftYawSpeed = 5f;
    public float driftYawReturnSpeed = 4f;

    [HideInInspector] public bool flipDisabled = false;
    [HideInInspector] public float currentDriftYaw = 0f;
    public bool IsFlipping => isFlipping;

    private ArcadeRunnerCarController carController;
    private WheelVisuals wheelVisuals;
    private Rigidbody rb;

    private bool isFlipping = false;
    private bool hasFlippedThisJump = false;
    private float flipAngleDone = 0f;
    private float flipDirection = 0f;
    private bool wasGrounded = true;
    private float currentAirLean = 0f;

    void Awake()
    {
        carController = GetComponent<ArcadeRunnerCarController>();
        wheelVisuals = GetComponent<WheelVisuals>();
        rb = GetComponent<Rigidbody>();
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;
        if (flipDisabled) return;
        if (!carController.IsGrounded) return;

        float steer = carController.steerInput;
        if (Mathf.Abs(steer) < 0.1f) return;
        if (hasFlippedThisJump) return;
        if (isFlipping) return;

        flipDirection = steer > 0 ? 1f : -1f;
        isFlipping = true;
        hasFlippedThisJump = true;
        flipAngleDone = 0f;

        if (wheelVisuals != null)
            wheelVisuals.isFlipping = true;
    }

    void LateUpdate() // Changed from Update to LateUpdate for smoother physics syncing
    {
        bool grounded = carController.IsGrounded;
        float steer = carController.steerInput;

        // 1. Handle Landing
        if (grounded && !wasGrounded)
        {
            hasFlippedThisJump = false;
            isFlipping = false;
            if (wheelVisuals != null)
                wheelVisuals.isFlipping = false;
        }
        wasGrounded = grounded;

        // 2. Calculate Drift & Lean
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float lateralSlip = localVel.x;
        float speedFactor = Mathf.Clamp01(Mathf.Abs(localVel.z) / 5f);

        float targetYaw = grounded
            ? steer * maxDriftYaw * speedFactor * Mathf.Clamp01(Mathf.Abs(lateralSlip) / 2f)
            : 0f;

        currentDriftYaw = Mathf.Lerp(
            currentDriftYaw,
            targetYaw,
            Time.deltaTime * (Mathf.Abs(targetYaw) > 0.1f ? driftYawSpeed : driftYawReturnSpeed)
        );

        float targetLean = !grounded && Mathf.Abs(steer) > 0.1f
            ? -steer * maxAirLean : 0f;

        currentAirLean = Mathf.Lerp(
            currentAirLean,
            targetLean,
            Time.deltaTime * (grounded ? airLeanReturnSpeed : airLeanSpeed)
        );

        // 3. Process the Flip Math
        if (isFlipping)
        {
            flipAngleDone += flipSpeed * Time.deltaTime;

            if (flipAngleDone >= 360f)
            {
                flipAngleDone = 0f;
                isFlipping = false;
                if (wheelVisuals != null)
                    wheelVisuals.isFlipping = false;
            }
        }
        else if (flipAngleDone > 0f)
        {
            flipAngleDone = Mathf.Lerp(flipAngleDone, 360f, Time.deltaTime * flipSmoothing);
            if (360f - flipAngleDone < 1f) flipAngleDone = 0f;
        }

        // 4. Combine Rotations (THE SMOOTH FIX)
        if (carVisuals != null)
        {
            // Instead of Euler, we use AngleAxis to prevent the 180-degree flip bug.
            Quaternion yawRotation = Quaternion.AngleAxis(currentDriftYaw, Vector3.up);

            // We combine the Lean and the Flip together since they both happen on the Z (Forward) axis
            float totalZRotation = currentAirLean + (flipAngleDone * flipDirection);
            Quaternion rollRotation = Quaternion.AngleAxis(totalZRotation, Vector3.forward);

            // Apply Yaw first, then Roll
            carVisuals.localRotation = yawRotation * rollRotation;
        }
    }
}