using UnityEngine;

public class CarAirTilt : MonoBehaviour
{
    [Header("References")]
    public Transform carVisuals;

    [Header("Tilt Settings")]
    public float maxTiltAngle = 25f;
    public float tiltSpeed = 5f;
    public float returnSpeed = 4f;

    private ArcadeRunnerCarController carController;
    private CarFlip carFlip;
    private float currentTilt = 0f;

    void Awake()
    {
        carController = GetComponent<ArcadeRunnerCarController>();
        carFlip = GetComponent<CarFlip>();
    }

    void Update()
    {
        if (carVisuals == null) return;

        // CarFlip owns Z rotation during flip — don't interfere
        if (carFlip != null && carFlip.IsFlipping) return;

        bool grounded = carController.IsGrounded;
        float steer = carController.steerInput;

        float targetTilt = 0f;

        if (!grounded && Mathf.Abs(steer) > 0.1f)
            targetTilt = -steer * maxTiltAngle;

        currentTilt = Mathf.Lerp(
            currentTilt,
            grounded ? 0f : targetTilt,
            Time.deltaTime * (grounded ? returnSpeed : tiltSpeed)
        );

        Vector3 euler = carVisuals.localEulerAngles;
        carVisuals.localEulerAngles = new Vector3(
            euler.x,
            euler.y,
            currentTilt
        );
    }
}