using UnityEngine;
using DG.Tweening;

public class BoostVisuals : MonoBehaviour
{
    [Header("References")]
    public Transform carVisuals;
    public ArcadeRunnerCarController carController;

    [Header("Stretch Settings")]
    public float stretchZ = 1.35f;   // how much to stretch forward
    public float squishXY = 0.82f;   // how much to squish sides
    public float stretchInTime = 0.15f;
    public float stretchOutTime = 0.25f;

    private bool wasBoostingLastFrame = false;
    private Tweener stretchTween;

    void Update()
    {
        if (carController == null || carVisuals == null) return;

        bool boosting = carController.IsBoosting;

        if (boosting && !wasBoostingLastFrame)
            OnBoostStart();
        else if (!boosting && wasBoostingLastFrame)
            OnBoostEnd();

        wasBoostingLastFrame = boosting;
    }

    void OnBoostStart()
    {
        carVisuals.DOKill();
        carVisuals.localScale = Vector3.one;

        // Punch first for that snap feel
        carVisuals.DOPunchScale(new Vector3(-0.1f, -0.1f, 0.25f), 0.2f, 5, 0.5f)
            .OnComplete(() =>
            {
                // Then hold the stretched shape for boost duration
                carVisuals.DOScale(
                    new Vector3(squishXY, squishXY, stretchZ),
                    stretchInTime
                ).SetEase(Ease.OutBack);
            });
    }

    void OnBoostEnd()
    {
        carVisuals.DOKill();
        // Spring back to normal with a little overshoot
        carVisuals.DOScale(Vector3.one, stretchOutTime)
            .SetEase(Ease.OutBack)
            .OnComplete(() => carVisuals.localScale = Vector3.one);
    }
}