using UnityEngine;

public class CarCameraTilt : MonoBehaviour
{
    [Header("Target")]
    public Rigidbody targetRb;

    [Header("Tilt Settings")]
    public float tiltAmount = 8f;
    public float tiltSmooth = 5f;

    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.localRotation;
    }

    void LateUpdate()
    {
        if (targetRb == null) return;

        // Get sideways movement
        Vector3 localVel =
            transform.InverseTransformDirection(
                targetRb.linearVelocity
            );

        // Calculate tilt
        float tilt =
            -localVel.x * tiltAmount;

        // Create target rotation
        Quaternion targetRot =
            initialRotation *
            Quaternion.Euler(0, 0, tilt);

        // Smooth tilt
        transform.localRotation =
            Quaternion.Lerp(
                transform.localRotation,
                targetRot,
                Time.deltaTime * tiltSmooth
            );
    }
}