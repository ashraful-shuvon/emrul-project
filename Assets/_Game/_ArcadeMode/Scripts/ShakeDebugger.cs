using UnityEngine;

public class ShakeDebugger : MonoBehaviour
{
    private Rigidbody rb;
    private Vector3 lastPosition;
    private Vector3 lastVelocity;
    private float lastY;

    [Header("Thresholds")]
    public float yJumpThreshold = 0.05f;        // Y position spike
    public float velocityChangeThreshold = 3f;   // sudden velocity change

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        // Detect sudden Y position change — means something pushed car up
        float yDelta = transform.position.y - lastY;
        if (Mathf.Abs(yDelta) > yJumpThreshold)
        {
            Debug.LogWarning($"SHAKE: Y position jumped by {yDelta:F4} " +
                             $"| pos={transform.position} " +
                             $"| vel={rb.linearVelocity}");
        }

        // Detect sudden velocity change — means collision impulse
        Vector3 velDelta = rb.linearVelocity - lastVelocity;
        if (velDelta.magnitude > velocityChangeThreshold)
        {
            Debug.LogWarning($"SHAKE: Velocity spiked by {velDelta.magnitude:F2} " +
                             $"| velDelta={velDelta} " +
                             $"| vel={rb.linearVelocity}");
        }

        lastY = transform.position.y;
        lastVelocity = rb.linearVelocity;
        lastPosition = transform.position;
    }

    void Update()
    {
        // Check what the car is touching
        Collider[] hits = Physics.OverlapSphere(
            transform.position, 1.5f
        );

        foreach (var hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            if (hit.gameObject.GetComponent<HexTile>() != null)
            {
                // How many hex tiles touching at once
                Debug.Log($"SHAKE: Touching hex tile {hit.gameObject.name} " +
                          $"at {hit.transform.position}");
            }
        }
    }
}