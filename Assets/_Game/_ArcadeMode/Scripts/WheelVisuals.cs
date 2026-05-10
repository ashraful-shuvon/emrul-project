using UnityEngine;

public class WheelVisuals : MonoBehaviour
{
    [System.Serializable]
    public class WheelPair
    {
        public WheelCollider collider;
        public Transform mesh;
        public bool flipY = false;
        public bool steers = false;
    }

    [Header("Wheels")]
    public WheelPair frontLeft;
    public WheelPair frontRight;
    public WheelPair rearLeft;
    public WheelPair rearRight;

    [Header("Steering")]
    public float maxSteerAngle = 30f;

    [HideInInspector] public bool isFlipping = false;

    private Rigidbody rb;
    private ArcadeRunnerCarController carController;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        carController = GetComponent<ArcadeRunnerCarController>();
    }

    void Update()
    {
        // Stop overriding wheel positions during flip OR while airborne
        // so CarVisuals tilt/flip carries all meshes freely
        if (isFlipping || !carController.IsGrounded) return;

        UpdateWheel(frontLeft);
        UpdateWheel(frontRight);
        UpdateWheel(rearLeft);
        UpdateWheel(rearRight);
    }

    void UpdateWheel(WheelPair wheel)
    {
        if (wheel.collider == null || wheel.mesh == null) return;

        if (wheel.steers)
            wheel.collider.steerAngle = carController.steerInput * maxSteerAngle;

        wheel.collider.GetWorldPose(out Vector3 position, out Quaternion rotation);

        wheel.mesh.position = position;

        if (wheel.flipY)
            rotation *= Quaternion.Euler(0f, 180f, 0f);

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