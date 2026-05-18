using UnityEngine;

public class DriftParticles : MonoBehaviour
{
    [Header("Particles")]
    public ParticleSystem driftLeft;
    public ParticleSystem driftRight;

    [Header("Settings")]
    public float steerThreshold = 0.3f;
    public float speedThreshold = 5f;
    public float cooldown = 3f;

    private ArcadeRunnerCarController carController;
    private Rigidbody rb;
    private bool wasSteering = false;
    private bool wasDoubleJumping = false;
    private float cooldownTimer = 0f;

    void Awake()
    {
        carController = GetComponent<ArcadeRunnerCarController>();
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        float steer = carController.steerInput;
        float speed = rb.linearVelocity.magnitude;
        bool isDoubleJumping = carController.isDoubleJumping;

        bool isSteering = Mathf.Abs(steer) > steerThreshold
                          && speed > speedThreshold;

        bool canTrigger = carController.IsGrounded || isDoubleJumping;

        // Trigger on steer
        if (isSteering && !wasSteering && cooldownTimer <= 0f && canTrigger)
        {
            PlayBoth();
            cooldownTimer = cooldown;
        }

        // Trigger on double jump start
        if (isDoubleJumping && !wasDoubleJumping && cooldownTimer <= 0f)
        {
            PlayBoth();
            cooldownTimer = cooldown;
        }

        wasSteering = isSteering;
        wasDoubleJumping = isDoubleJumping;
    }

    void PlayBoth()
    {
        if (driftLeft != null) { driftLeft.Stop(); driftLeft.Play(); }
        if (driftRight != null) { driftRight.Stop(); driftRight.Play(); }
    }
}