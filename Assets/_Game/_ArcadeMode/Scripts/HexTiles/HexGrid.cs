using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class HexGrid : MonoBehaviour, IHexGrid
{
    [Header("Prefab")]
    public GameObject hexPrefab;

    [Header("Arena Shape")]
    public int arenaRadius = 10;

    [Header("Hex Size")]
    public float hexRadius = 1f;

    [Header("Rear Wheel Triggers")]
    public Transform rearLeftWheel;
    public Transform rearRightWheel;
    public float triggerRadius = 1.5f;

    [Header("Trigger Cooldown")]
    [Tooltip("Seconds a tile is immune to re-triggering after first contact.")]
    public float triggerCooldown = 0.15f;

    [Header("Respawn")]
    public float respawnDelay = 1f;

    private Dictionary<Vector2Int, HexTile> activeTiles
        = new Dictionary<Vector2Int, HexTile>();
    private Dictionary<Vector2Int, Vector3> tilePositions
        = new Dictionary<Vector2Int, Vector3>();
    private Queue<GameObject> pool = new Queue<GameObject>();
    private Dictionary<Vector2Int, float> triggerTimestamps
        = new Dictionary<Vector2Int, float>();

    void Start()
    {
        GenerateRoundArena();
    }

    void GenerateRoundArena()
    {
        float r = hexRadius;
        float colStepX = r * Mathf.Sqrt(3f);
        float rowStepZ = r * 1.5f;
        float evenRowOffsetX = colStepX * 0.5f;

        int size = arenaRadius * 2 + 2;
        float arenaWorldRadius = arenaRadius * colStepX * 0.5f;

        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                float x = col * colStepX + (row % 2 == 1 ? evenRowOffsetX : 0f);
                float z = row * rowStepZ;

                float centerX = (size * colStepX) * 0.5f;
                float centerZ = (size * rowStepZ) * 0.5f;

                float worldX = x - centerX;
                float worldZ = z - centerZ;

                if (Mathf.Sqrt(worldX * worldX + worldZ * worldZ) > arenaWorldRadius)
                    continue;

                Vector3 pos = transform.position + new Vector3(worldX, 0f, worldZ);
                Vector2Int key = new Vector2Int(col, row);
                tilePositions[key] = pos;
                SpawnTile(key, pos);
            }
        }
    }

    void SpawnTile(Vector2Int key, Vector3 pos)
    {
        GameObject obj;
        if (pool.Count > 0)
            obj = pool.Dequeue();
        else
            obj = Instantiate(hexPrefab, pos, Quaternion.identity, transform);

        obj.SetActive(true);
        HexTile tile = obj.GetComponent<HexTile>();
        if (tile == null) tile = obj.AddComponent<HexTile>();
        tile.Init(pos, this, key);
        activeTiles[key] = tile;
        triggerTimestamps.Remove(key);
    }

    public void ScheduleRespawn(Vector2Int key)
    {
        StartCoroutine(RespawnAfterDelay(key));
    }

    IEnumerator RespawnAfterDelay(Vector2Int key)
    {
        yield return new WaitForSeconds(respawnDelay);
        RespawnTile(key);
    }

    void RespawnTile(Vector2Int key)
    {
        if (!tilePositions.ContainsKey(key)) return;

        if (activeTiles.TryGetValue(key, out HexTile oldTile) && oldTile != null)
        {
            oldTile.gameObject.SetActive(false);
            pool.Enqueue(oldTile.gameObject);
            activeTiles.Remove(key);
        }

        SpawnTile(key, tilePositions[key]);
    }

    // Moved from Update → FixedUpdate to stay in sync with physics
    void FixedUpdate()
    {
        CheckWheelOverTiles(rearLeftWheel);
        CheckWheelOverTiles(rearRightWheel);
    }

    void CheckWheelOverTiles(Transform wheel)
    {
        if (wheel == null) return;

        float now = Time.fixedTime;
        List<Vector2Int> toTrigger = new List<Vector2Int>();

        foreach (var kvp in activeTiles)
        {
            if (kvp.Value == null || !kvp.Value.gameObject.activeSelf) continue;
            if (kvp.Value.transform.position.y < -100f) continue;
            if (kvp.Value.IsTriggered) continue;

            if (triggerTimestamps.TryGetValue(kvp.Key, out float lastTime) &&
                now - lastTime < triggerCooldown)
                continue;

            float dist = Vector3.Distance(
                new Vector3(wheel.position.x, 0f, wheel.position.z),
                new Vector3(kvp.Value.transform.position.x, 0f, kvp.Value.transform.position.z)
            );

            if (dist < triggerRadius)
                toTrigger.Add(kvp.Key);
        }

        foreach (var key in toTrigger)
        {
            if (activeTiles.ContainsKey(key))
            {
                triggerTimestamps[key] = now;
                activeTiles[key].TriggerFall();
            }
        }
    }
}