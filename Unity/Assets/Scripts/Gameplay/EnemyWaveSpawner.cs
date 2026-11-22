using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class EnemyWaveSpawner : MonoBehaviour
{
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Camera sourceCamera;
    [SerializeField] private Transform spawnParent;
    [SerializeField] private bool useEnemyPrefabZPosition = true;
    [SerializeField] private float spawnZPosition = 0f;
    [SerializeField] private float edgeViewportPadding = 0.08f;
    [SerializeField] private Vector2 spawnIntervalRange = new Vector2(0.8f, 1.6f);

    private Coroutine _spawnRoutine;

    private void OnEnable()
    {
        _spawnRoutine = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (_spawnRoutine == null)
        {
            return;
        }

        StopCoroutine(_spawnRoutine);
        _spawnRoutine = null;
    }

    private IEnumerator SpawnLoop()
    {
        while (enabled)
        {
            var waitTime = Random.Range(spawnIntervalRange.x, spawnIntervalRange.y);
            if (waitTime > 0f)
            {
                yield return new WaitForSeconds(waitTime);
            }

            TrySpawnEnemy();
        }
    }

    private void TrySpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            return;
        }

        var activeCamera = sourceCamera != null ? sourceCamera : Camera.main;
        if (activeCamera == null)
        {
            return;
        }

        var spawnZ = useEnemyPrefabZPosition ? enemyPrefab.transform.position.z : spawnZPosition;
        if (!TryBuildEdgeSpawnPosition(activeCamera, spawnZ, out var spawnPosition))
        {
            return;
        }

        Instantiate(enemyPrefab, spawnPosition, enemyPrefab.transform.rotation, spawnParent);
    }

    private bool TryBuildEdgeSpawnPosition(Camera activeCamera, float spawnZ, out Vector3 spawnPosition)
    {
        var side = Random.Range(0, 4);
        var sideT = Random.value;

        float viewportX;
        float viewportY;
        switch (side)
        {
            case 0: // Left
                viewportX = -edgeViewportPadding;
                viewportY = sideT;
                break;
            case 1: // Right
                viewportX = 1f + edgeViewportPadding;
                viewportY = sideT;
                break;
            case 2: // Bottom
                viewportX = sideT;
                viewportY = -edgeViewportPadding;
                break;
            default: // Top
                viewportX = sideT;
                viewportY = 1f + edgeViewportPadding;
                break;
        }

        var ray = activeCamera.ViewportPointToRay(new Vector3(viewportX, viewportY, 0f));
        var spawnPlane = new Plane(Vector3.forward, new Vector3(0f, 0f, spawnZ));
        if (spawnPlane.Raycast(ray, out var enter))
        {
            spawnPosition = ray.GetPoint(enter);
            spawnPosition.z = spawnZ;
            return true;
        }

        spawnPosition = Vector3.zero;
        return false;
    }

    private void OnValidate()
    {
        if (spawnIntervalRange.x > spawnIntervalRange.y)
        {
            (spawnIntervalRange.x, spawnIntervalRange.y) = (spawnIntervalRange.y, spawnIntervalRange.x);
        }

        spawnIntervalRange.x = Mathf.Max(0.01f, spawnIntervalRange.x);
        spawnIntervalRange.y = Mathf.Max(spawnIntervalRange.x, spawnIntervalRange.y);
        edgeViewportPadding = Mathf.Max(0f, edgeViewportPadding);
    }
}
