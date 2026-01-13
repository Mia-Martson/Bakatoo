using System.Collections.Generic;
using UnityEngine;

public class FlowerSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Usually the XR camera (Main Camera) under the XR Origin.")]
    public Transform user;

    [Header("Spawning")]
    public GameObject[] flowerPrefabs;
    public float spawnRadius = 2.0f;
    public float minDistanceFromUser = 0.8f;
    public float spawnHeight = 0.0f; // if no floor available, use this relative height (e.g. 0)
    public int flowersPerStep = 3;

    [Header("Movement Trigger")]
    [Tooltip("Spawn when the user moved this many meters since last spawn.")]
    public float distancePerSpawn = 0.75f;

    [Header("Limits")]
    public int maxFlowers = 250;
    public bool destroyOldestWhenFull = true;

    [Header("Placement")]
    [Tooltip("If set, will raycast down to find floor/plane/mesh.")]
    public LayerMask floorMask = ~0; // default: everything

    [Tooltip("How far down to search for floor from the sample point.")]
    public float floorRaycastDistance = 3.0f;

    private Vector3 _lastSpawnUserPos;
    private readonly Queue<GameObject> _spawned = new Queue<GameObject>();

    void Start()
    {
        if (user == null)
        {
            // Try to find the Main Camera automatically.
            var cam = Camera.main;
            if (cam != null) user = cam.transform;
        }

        if (user == null)
        {
            Debug.LogError("FlowerSpawner: No user Transform assigned and no Main Camera found.");
            enabled = false;
            return;
        }

        _lastSpawnUserPos = user.position;
    }

    void Update()
    {
        float moved = Vector3.Distance(user.position, _lastSpawnUserPos);
        if (moved >= distancePerSpawn)
        {
            SpawnBatch();
            _lastSpawnUserPos = user.position;
        }
    }

    void SpawnBatch()
    {
        if (flowerPrefabs == null || flowerPrefabs.Length == 0)
        {
            Debug.LogWarning("FlowerSpawner: No flowerPrefabs assigned.");
            return;
        }

        for (int i = 0; i < flowersPerStep; i++)
        {
            Vector3 sample = RandomPointAroundUser();

            // Try to place on floor by raycasting down.
            Vector3 placePos = sample;
            if (TryProjectToFloor(sample, out Vector3 floorPos))
                placePos = floorPos;
            else
                placePos.y = user.position.y + spawnHeight;

            // Spawn
            var prefab = flowerPrefabs[Random.Range(0, flowerPrefabs.Length)];
            var rot = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            GameObject flower = Instantiate(prefab, placePos, rot);

            // Optional: small random scale variation
            float s = Random.Range(0.85f, 1.25f);
            flower.transform.localScale *= s;

            EnforceLimit(flower);
        }
    }

    Vector3 RandomPointAroundUser()
    {
        // Random point in a ring around user on XZ plane
        Vector2 dir = Random.insideUnitCircle.normalized;
        float r = Random.Range(minDistanceFromUser, spawnRadius);
        Vector3 offset = new Vector3(dir.x, 0f, dir.y) * r;
        return user.position + offset;
    }

    bool TryProjectToFloor(Vector3 from, out Vector3 floorPos)
    {
        // Cast from slightly above the sample point downwards
        Vector3 origin = from + Vector3.up * 1.5f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, floorRaycastDistance, floorMask, QueryTriggerInteraction.Ignore))
        {
            floorPos = hit.point;
            return true;
        }

        floorPos = default;
        return false;
    }

    void EnforceLimit(GameObject newest)
    {
        _spawned.Enqueue(newest);

        while (_spawned.Count > maxFlowers)
        {
            if (!destroyOldestWhenFull) break;
            var old = _spawned.Dequeue();
            if (old != null) Destroy(old);
        }
    }
}
