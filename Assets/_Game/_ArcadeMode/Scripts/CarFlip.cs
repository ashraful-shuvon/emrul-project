using UnityEngine;
using UnityEngine.InputSystem;

public class CarFlip : MonoBehaviour
{
    [Header("References")]
    public Transform carVisuals;

    [Header("Flip Settings")]
    public float flipSpeed = 720f;

    [HideInInspector] public bool flipDisabled = false;
    public bool IsFlipping => isFlipping;

    private ArcadeRunnerCarController carController;
    private WheelVisuals wheelVisuals;

    private bool isFlipping = false;
    private bool hasFlippedThisJump = false;
    private float flipAngleDone = 0f;
    private float flipDirection = 0f;
    private bool wasGrounded = true;

    void Awake()
    {
        carController = GetComponent<ArcadeRunnerCarController>();
        wheelVisuals = GetComponent<WheelVisuals>();
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;
        if (flipDisabled) return;
        if (!carController.IsGrounded) return; // only on first jump from ground

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

        if (grounded && !wasGrounded)
        {
            hasFlippedThisJump = false;

            if (carVisuals != null)
            {
                Vector3 e = carVisuals.localEulerAngles;
                carVisuals.localEulerAngles = new Vector3(e.x, e.y, 0f);
            }

            if (wheelVisuals != null)
                wheelVisuals.isFlipping = false;
        }

        wasGrounded = grounded;

        if (isFlipping)
            ProcessFlip();
    }

    void ProcessFlip()
    {
        if (carVisuals == null) return;

        flipAngleDone += flipSpeed * Time.deltaTime;

        if (flipAngleDone >= 360f)
        {
            isFlipping = false;
            flipAngleDone = 0f;

            Vector3 e = carVisuals.localEulerAngles;
            carVisuals.localEulerAngles = new Vector3(e.x, e.y, 0f);

            if (wheelVisuals != null)
                wheelVisuals.isFlipping = false;

            return;
        }

        Vector3 euler = carVisuals.localEulerAngles;
        carVisuals.localEulerAngles = new Vector3(
            euler.x,
            euler.y,
            flipAngleDone * flipDirection
        );
    }
}