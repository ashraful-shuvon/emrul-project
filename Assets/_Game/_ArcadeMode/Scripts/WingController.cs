using UnityEngine;

public class WingController : MonoBehaviour
{
    private Animator animator;
    private ArcadeRunnerCarController carController;
    private CarFlip carFlip;

    private bool wingsOpen = false;
    private bool wasDoubleJumping = false;

    void Awake()
    {
        animator = GetComponent<Animator>();
        carController = GetComponentInParent<ArcadeRunnerCarController>();
        carFlip = GetComponentInParent<CarFlip>();
    }

    void Update()
    {
        bool doubleJumping = carController.isDoubleJumping;
        bool grounded = carController.IsGrounded;
        bool wingsClosed = carController.wingsClosed;

        // Open wings on second jump
        if (doubleJumping && !wasDoubleJumping)
            SetWings(true);

        // Close wings on third press
        if (wingsClosed && wingsOpen)
            SetWings(false);

        // Close wings on landing
        if (grounded && wingsOpen)
            SetWings(false);

        // Disable flip while wings open
        if (carFlip != null)
            carFlip.flipDisabled = wingsOpen;

        wasDoubleJumping = doubleJumping;
    }

    void SetWings(bool open)
    {
        wingsOpen = open;
        animator.SetBool("Enabled", open);
    }
}