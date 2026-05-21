using UnityEngine;

public class HexTile : MonoBehaviour
{
    [Header("Fall Settings")]
    public float fallSpeed = 8f;
    public float fallDistance = 10f;

    private Vector3 startPosition;
    private bool isTriggered = false;
    private IHexGrid grid;
    private Vector2Int gridKey;

    public void Init(Vector3 position, IHexGrid hexGrid, Vector2Int key)
    {
        startPosition = position;
        transform.position = position;
        grid = hexGrid;
        gridKey = key;
        isTriggered = false;
        StopAllCoroutines();
        gameObject.SetActive(true);
    }

    public void TriggerFall()
    {
        if (isTriggered) return;
        isTriggered = true;
        StartCoroutine(FallSequence());
    }

    System.Collections.IEnumerator FallSequence()
    {
        // Shake
        Vector3 originalPos = transform.position;
        float elapsed = 0f;

        while (elapsed < 0.2f)
        {
            float shake = Mathf.Sin(elapsed * 40f) * 0.04f;
            transform.position = originalPos + new Vector3(shake, 0f, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;

        // Fall down
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

        // Move far below — keeps coroutine alive unlike SetActive(false)
        transform.position = new Vector3(
            transform.position.x,
            startPosition.y - 1000f,
            transform.position.z
        );

        // Notify grid — grid handles respawn timer
        grid?.ScheduleRespawn(gridKey);
    }
}