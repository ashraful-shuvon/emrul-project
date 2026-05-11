using UnityEngine;
using UnityEngine.InputSystem;

public class CarFlip : MonoBehaviour
{
    [Header("References")]
    public Transform carVisuals;

    [Header("Flip Settings")]
    public float flipSpeed = 720f;

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

    void Update()
    {
        bool grounded = carController.IsGrounded;
        float steer = carController.steerInput;

        if (grounded && !wasGrounded)
        {
            hasFlippedThisJump = false;
            if (wheelVisuals != null)
                wheelVisuals.isFlipping = false;
        }

        wasGrounded = grounded;

        // Flip owns everything — skip other visuals
        if (isFlipping)
        {
            ProcessFlip();
            return;
        }

        // ── DRIFT YAW (Y) — ground only ─────────────
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float lateralSlip = localVel.x;
        float speedFactor = Mathf.Clamp01(Mathf.Abs(localVel.z) / 5f);

        float targetYaw = grounded
            ? steer * maxDriftYaw * speedFactor
              * Mathf.Clamp01(Mathf.Abs(lateralSlip) / 2f)
            : 0f;

        currentDriftYaw = Mathf.Lerp(
            currentDriftYaw,
            targetYaw,
            Time.deltaTime * (Mathf.Abs(targetYaw) > 0.1f
                ? driftYawSpeed : driftYawReturnSpeed)
        );

        // ── AIR LEAN (Z) — air only ──────────────────
        float targetLean = !grounded && Mathf.Abs(steer) > 0.1f
            ? -steer * maxAirLean : 0f;

        currentAirLean = Mathf.Lerp(
            currentAirLean,
            targetLean,
            Time.deltaTime * (grounded ? airLeanReturnSpeed : airLeanSpeed)
        );

        // ── SINGLE WRITE — nothing else touches carVisuals ──
        if (carVisuals != null)
            carVisuals.localEulerAngles = new Vector3(
                0f,
                currentDriftYaw,
                currentAirLean
            );
    }

    void ProcessFlip()
    {
        if (carVisuals == null) return;

        flipAngleDone += flipSpeed * Time.deltaTime;

        if (flipAngleDone >= 360f)
        {
            isFlipping = false;
            flipAngleDone = 0f;
            currentAirLean = 0f;
            currentDriftYaw = 0f;

            if (wheelVisuals != null)
                wheelVisuals.isFlipping = false;

            carVisuals.localEulerAngles = Vector3.zero;
            return;
        }

        carVisuals.localEulerAngles = new Vector3(
            0f,
            0f,
            flipAngleDone * flipDirection
        );
    }
}