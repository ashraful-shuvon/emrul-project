using UnityEngine;

public class TireSmokeController : MonoBehaviour
{
    [Header("Particles")]
    public ParticleSystem leftSmoke;
    public ParticleSystem rightSmoke;

    [Header("Settings")]
    public float steerThreshold = 0.3f;
    public float speedThreshold = 3f;

    private ArcadeRunnerCarController carController;
    private Rigidbody rb;

    void Awake()
    {
        carController = GetComponent<ArcadeRunnerCarController>();
        rb = GetComponent<Rigidbody>();

        ApplyCarColorToSmoke();
        StopSmoke();
    }

    void ApplyCarColorToSmoke()
    {
        ColorUtility.TryParseHtmlString("#20B320", out Color smokeColor);
        smokeColor.a = 0.6f;

        SetSmokeColor(leftSmoke, smokeColor);
        SetSmokeColor(rightSmoke, smokeColor);
    }

    void SetSmokeColor(ParticleSystem ps, Color color)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startColor = new ParticleSystem.MinMaxGradient(color);
    }

    void Update()
    {
        bool grounded = carController.IsGrounded;
        float steer = carController.steerInput;
        float speed = rb.linearVelocity.magnitude;

        bool isTurning = Mathf.Abs(steer) > steerThreshold
                         && speed > speedThreshold
                         && grounded;

        if (isTurning) PlaySmoke();
        else StopSmoke();
    }

    void PlaySmoke()
    {
        if (leftSmoke != null && !leftSmoke.isPlaying) leftSmoke.Play();
        if (rightSmoke != null && !rightSmoke.isPlaying) rightSmoke.Play();
    }

    void StopSmoke()
    {
        if (leftSmoke != null && leftSmoke.isPlaying) leftSmoke.Stop();
        if (rightSmoke != null && rightSmoke.isPlaying) rightSmoke.Stop();
    }
}