using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class CarFlip : MonoBehaviour
{
    [Header("References")]
    public Transform carVisuals;

    [Header("Flip Settings")]
    public float flipSpeed = 720f;

    private ArcadeRunnerCarController carController;
    private WheelVisuals wheelVisuals;

    private bool isFlipping = false;
    private bool hasFlippedThisJump = false;
    private float flipAngleDone = 0f;
    private float flipDirection = 0f;
    private bool wasGrounded = true;
    public bool IsFlipping => isFlipping;

    void Awake()
    {
        carController = GetComponent<ArcadeRunnerCarController>();
        wheelVisuals = GetComponent<WheelVisuals>();
    }

    public void OnJump(InputValue value)
    {
        if (!value.isPressed) return;

        float steer = carController.steerInput;

        if (Mathf.Abs(steer) < 0.1f) return;
        if (hasFlippedThisJump) return;
        if (isFlipping) return;

        flipDirection = steer > 0 ? 1f : -1f;
        isFlipping = true;
        hasFlippedThisJump = true;
        flipAngleDone = 0f;

        // REPLACE with:
        if (carVisuals != null)
        {
            carVisuals.DOKill();
            carVisuals.localScale = Vector3.one; // reset before punch so it always starts clean
            carVisuals.DOPunchScale(new Vector3(0.2f, -0.3f, 0.2f), 0.25f, 5, 0.5f)
                .OnComplete(() => carVisuals.localScale = Vector3.one);
        }

        // Tell WheelVisuals to stop overriding meshes
        if (wheelVisuals != null)
            wheelVisuals.isFlipping = true;
    }

    void Update()
    {
        bool grounded = carController.IsGrounded;

        if (grounded && !wasGrounded)
        {
            hasFlippedThisJump = false;

            // Snap roll back to zero on landing
            if (carVisuals != null)
            {
                Vector3 e = carVisuals.localEulerAngles;
                carVisuals.localEulerAngles = new Vector3(e.x, e.y, 0f);
            }

            // Resume wheel visuals
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

        float step = flipSpeed * Time.deltaTime;
        flipAngleDone += step;

        if (carVisuals != null)
        {
            carVisuals.DOKill();
            carVisuals.localScale = Vector3.one; // hard reset after flip done
        }

        if (flipAngleDone >= 360f)
        {
            isFlipping = false;
            flipAngleDone = 0f;

            // Snap to zero and resume wheels
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