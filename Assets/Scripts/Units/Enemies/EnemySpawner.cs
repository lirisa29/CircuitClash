using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyWave
{
    public string waveName;
    public List<EnemyGroup> enemyGroups;
    public float timeBeforeNextWave = 5f;
}

[System.Serializable]
public class EnemyGroup
{
    public GameObject enemyPrefab;
    public int count;
    public float spawnInterval;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Waves")]
    public List<EnemyWave> waves = new List<EnemyWave>();
    private int currentWaveIndex = 0;
    public int CurrentWave => currentWaveIndex + 1;

    [Header("Dependencies")]
    private GridManager gridManager;

    [Header("Formation Settings")]
    public float enemySpacing = 1f; // spacing between enemies in a column
    
    IEnumerator Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();

        // Wait until GridManager has generated paths
        while (gridManager.enemyPaths.Count == 0)
        {
            yield return null; // wait one frame
        }

        if (waves.Count > 0)
        {
            StartCoroutine(SpawnWave(waves[currentWaveIndex]));
        }
    }

    IEnumerator SpawnWave(EnemyWave wave)
    {
        foreach (EnemyGroup group in wave.enemyGroups)
        {
            if (gridManager.enemyPaths.Count == 0) yield break;
            int pathIndex = Random.Range(0, gridManager.enemyPaths.Count);
            List<Vector2Int> path = gridManager.enemyPaths[pathIndex];

            // create a shared stop flag for this group
            BoolWrapper groupStopFlag = new BoolWrapper();

            for (int i = 0; i < group.count; i++)
            {
                SpawnEnemy(group.enemyPrefab, path, i, groupStopFlag);
                yield return new WaitForSeconds(group.spawnInterval);
            }
        }

        yield return new WaitForSeconds(wave.timeBeforeNextWave);

        currentWaveIndex++;
        if (currentWaveIndex < waves.Count)
            StartCoroutine(SpawnWave(waves[currentWaveIndex]));
        else
        {
            currentWaveIndex = 0;
            StartCoroutine(SpawnWave(waves[currentWaveIndex]));
        }
    }

    void SpawnEnemy(GameObject enemyPrefab, List<Vector2Int> path, int columnIndex, BoolWrapper groupStopFlag)
    {
        Vector3 spawnPos = gridManager.GridToWorld(path[0]);
        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        enemy.GetComponent<EnemyMovement>().InitPath(path, gridManager, columnIndex * enemySpacing, groupStopFlag);
    }
}
