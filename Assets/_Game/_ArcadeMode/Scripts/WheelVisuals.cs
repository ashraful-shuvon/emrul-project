using UnityEngine;

public class WheelVisuals : MonoBehaviour
{
    [System.Serializable]
    public class WheelPair
    {
        public WheelCollider collider;
        public Transform mesh;
        public bool flipY = false;
        public bool steers = false; // enable for front wheels only
    }

    [Header("Wheels")]
    public WheelPair frontLeft;
    public WheelPair frontRight;
    public WheelPair rearLeft;
    public WheelPair rearRight;

    [Header("Steering")]
    public float maxSteerAngle = 30f;

    private Rigidbody rb;
    private ArcadeRunnerCarController carController;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        carController = GetComponent<ArcadeRunnerCarController>();
    }

    void Update()
    {
        UpdateWheel(frontLeft);
        UpdateWheel(frontRight);
        UpdateWheel(rearLeft);
        UpdateWheel(rearRight);
    }

    void UpdateWheel(WheelPair wheel)
    {
        if (wheel.collider == null || wheel.mesh == null) return;

        // Feed steer angle into WheelCollider so GetWorldPose returns correct rotation
        if (wheel.steers)
        {
            wheel.collider.steerAngle =
                carController.steerInput * maxSteerAngle;
        }

        wheel.collider.GetWorldPose(out Vector3 position, out Quaternion rotation);

        wheel.mesh.position = position;

        if (wheel.flipY)
            rotation *= Quaternion.Euler(0f, 180f, 0f);

        // Manual spin from velocity since WheelCollider isn't motorized
        float rpm = wheel.collider.rpm;
        if (Mathf.Abs(rpm) < 1f && rb != null)
        {
            float speed = rb.linearVelocity.magnitude;
            float circumference = 2f * Mathf.PI * wheel.collider.radius;
            float rotationsPerSecond = speed / circumference;
            float spinAngle = rotationsPerSecond * 360f * Time.deltaTime;
            rotation *= Quaternion.Euler(spinAngle, 0f, 0f);
        }

        wheel.mesh.rotation = rotation;
    }
}