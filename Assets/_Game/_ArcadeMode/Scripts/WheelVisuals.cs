using UnityEngine;
using DG.Tweening;

public class WheelVisuals : MonoBehaviour
{
    [System.Serializable]
    public class WheelPair
    {
        public WheelCollider collider;
        public Transform mesh;
        public bool flipY = false;
        public bool steers = false;
        // Cached local pose when leaving ground
        [HideInInspector] public Vector3 lastLocalPos;
        [HideInInspector] public Quaternion lastLocalRot;
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
        if (isFlipping) return;

        if (!carController.IsGrounded)
        {
            // In air — lock wheels to last known local pose
            // They follow CarVisuals naturally as children
            ApplyLocalPose(frontLeft);
            ApplyLocalPose(frontRight);
            ApplyLocalPose(rearLeft);
            ApplyLocalPose(rearRight);
            return;
        }

        // Grounded — update normally and cache local pose
        UpdateWheel(frontLeft);
        UpdateWheel(frontRight);
        UpdateWheel(rearLeft);
        UpdateWheel(rearRight);
    }

    void ApplyLocalPose(WheelPair wheel)
    {
        if (wheel.mesh == null) return;
        wheel.mesh.localPosition = wheel.lastLocalPos;
        wheel.mesh.localRotation = wheel.lastLocalRot;
    }

    void UpdateWheel(WheelPair wheel)
    {
        if (wheel.collider == null || wheel.mesh == null) return;

        if (wheel.steers)
            wheel.collider.steerAngle = carController.steerInput * maxSteerAngle;

        wheel.collider.GetWorldPose(out Vector3 position, out Quaternion rotation);

        // Apply drift yaw offset
        float yawOffset = carController.currentDriftYaw;
        if (Mathf.Abs(yawOffset) > 0.01f)
        {
            Vector3 localPos = transform.InverseTransformPoint(position);
            Quaternion yawRot = Quaternion.Euler(0f, yawOffset, 0f);
            localPos = yawRot * localPos;
            position = transform.TransformPoint(localPos);
            rotation = transform.rotation *
                       yawRot *
                       Quaternion.Inverse(transform.rotation) *
                       rotation;
        }

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

        // Cache local pose for air use
        wheel.lastLocalPos = wheel.mesh.localPosition;
        wheel.lastLocalRot = wheel.mesh.localRotation;
    }

    public void PlayLandingWobble()
    {
        WobbleWheel(frontLeft.mesh);
        WobbleWheel(frontRight.mesh);
        WobbleWheel(rearLeft.mesh);
        WobbleWheel(rearRight.mesh);
    }

    void WobbleWheel(Transform mesh)
    {
        if (mesh == null) return;
        mesh.DOKill();
        mesh.DOPunchPosition(Vector3.up * 0.06f, 0.2f, 6, 0.4f);
    }
}