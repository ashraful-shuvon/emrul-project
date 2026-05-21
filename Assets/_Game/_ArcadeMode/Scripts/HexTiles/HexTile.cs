using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Fall Settings")]
    public float fallSpeed = 8f;
    public float fallDistance = 10f;

    [Header("Layer Names")]
    public string defaultLayerName = "Ground";
    public string fallingLayerName = "FallingTile";

    private Vector3 startPosition;
    private bool isTriggered = false;
    private IHexGrid grid;
    private Vector2Int gridKey;

    private int defaultLayer;
    private int fallingLayer;

    private Transform visualChild;

    public bool IsTriggered => isTriggered;

    public void Init(Vector3 position, IHexGrid hexGrid, Vector2Int key)
    {
        startPosition = position;
        transform.position = position;
        grid = hexGrid;
        gridKey = key;
        isTriggered = false;

        gameObject.layer = defaultLayer;

        StopAllCoroutines();

        if (visualChild != null)
            visualChild.localPosition = Vector3.zero;

        gameObject.SetActive(true);
    }

    void Awake()
    {
        defaultLayer = LayerMask.NameToLayer(defaultLayerName);
        fallingLayer = LayerMask.NameToLayer(fallingLayerName);
        visualChild = transform.childCount > 0 ? transform.GetChild(0) : null;
    }

    public void TriggerFall()
    {
        if (isTriggered) return;
        isTriggered = true;

        if (fallingLayer >= 0)
            gameObject.layer = fallingLayer;

        StartCoroutine(FallSequence());
    }

    System.Collections.IEnumerator FallSequence()
    {
        // Shake the visual child only — root transform stays locked
        // so the Rigidbody above never gets a contact-point shift
        float elapsed = 0f;
        float shakeDuration = 0.2f;
        Transform shakeTarget = visualChild != null ? visualChild : transform;

        while (elapsed < shakeDuration)
        {
            float shake = Mathf.Sin(elapsed * 40f) * 0.04f;
            shakeTarget.localPosition = new Vector3(shake, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        shakeTarget.localPosition = Vector3.zero;

        // Fall root transform — car should be off the tile by now
        float targetY = startPosition.y - fallDistance;
        while (transform.position.y > targetY)
        {
            transform.position = new Vector3(
                transform.position.x,
                Mathf.MoveTowards(transform.position.y, targetY, fallSpeed * Time.deltaTime),
                transform.position.z
            );
            yield return null;
        }

        // Park far below — keeps coroutine alive unlike SetActive(false)
        transform.position = new Vector3(
            transform.position.x,
            startPosition.y - 1000f,
            transform.position.z
        );

        grid?.ScheduleRespawn(gridKey);
    }
}