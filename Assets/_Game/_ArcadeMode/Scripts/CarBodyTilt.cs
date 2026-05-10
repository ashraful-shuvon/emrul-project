using UnityEngine;

public class CarBodyTilt : MonoBehaviour
{
    [Header("References")]
    public Transform carVisuals;

    [Header("Drift Body Rotation")]
    public float maxDriftYaw = 15f;     // how many degrees rear kicks out
    public float yawSpeed = 5f;         // how fast it kicks
    public float returnSpeed = 4f;      // how fast it returns

    private ArcadeRunnerCarController carController;
    private CarFlip carFlip;
    private Rigidbody rb;

    private float currentYaw = 0f;

    void Awake()
    {
        carController = GetComponent<ArcadeRunnerCarController>();
        carFlip = GetComponent<CarFlip>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (carVisuals == null) return;
        if (carFlip != null && carFlip.IsFlipping) return;

        bool grounded = carController.IsGrounded;
        float steer = carController.steerInput;

        // Get lateral slip — how much car is sliding sideways
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);
        float lateralSlip = localVel.x;
        float speedFactor = Mathf.Clamp01(Mathf.Abs(localVel.z) / 5f);

        // Rear kicks opposite to steer — right turn = rear slides left = negative Y
        float targetYaw = grounded
            ? -steer * maxDriftYaw * speedFactor * Mathf.Clamp01(Mathf.Abs(lateralSlip) / 2f)
            : 0f;

        currentYaw = Mathf.Lerp(
            currentYaw,
            targetYaw,
            Time.deltaTime * (Mathf.Abs(targetYaw) > 0.1f ? yawSpeed : returnSpeed)
        );

        Vector3 euler = carVisuals.localEulerAngles;
        carVisuals.localEulerAngles = new Vector3(
            euler.x,
            currentYaw,     // Y — rear kicks out
            euler.z
        );
    }
}