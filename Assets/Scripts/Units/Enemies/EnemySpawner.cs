using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum EnemyTier { Easy, Medium, Hard }

[System.Serializable]
public class EnemyType
{
    public GameObject prefab;
    public int cost = 1;
    public float spawnInterval = 1f;
    public EnemyTier tier;
}

public class WaveDNA
{
    public float aggressiveness = 1.0f;
    public float spawnDensity = 1.0f;

    public void Mutate(float rate, float playerDominated)
    {
        // Clamp performance score to [-1, 1]
        playerDominated = Mathf.Clamp(playerDominated, -1f, 1f);

        // The bigger the score, the more aggressive the mutation
        float agDelta = rate * Mathf.Abs(playerDominated);
        float sdDelta = rate * Mathf.Abs(playerDominated);

        // Direction based on sign of performance
        float agChange = agDelta * Mathf.Sign(playerDominated);
        float sdChange = sdDelta * Mathf.Sign(playerDominated);

        aggressiveness += agChange;
        spawnDensity += sdChange;

        aggressiveness = Mathf.Clamp(aggressiveness, 0.5f, 2f);
        spawnDensity = Mathf.Clamp(spawnDensity, 0.5f, 2f);
    }
}

public class EnemyWave
{
    public string waveName;
    public List<EnemyGroup> enemyGroups = new List<EnemyGroup>();
    public float timeBeforeNextWave = 5f;
}

public class EnemyGroup
{
    public GameObject enemyPrefab;
    public int count;
    public float spawnInterval;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Procedural Wave Generation")]
    public List<EnemyType> enemyTypes;
    public AnimationCurve difficultyCurve;
    public int baseWaveBudget = 10;
    public float mutationRate = 0.1f;

    private WaveDNA currentDNA = new WaveDNA();
    private int currentWaveIndex = 0;
    private int wavesSurvived = 0;
    public int CurrentWave => currentWaveIndex + 1;
    public int WavesSurvived => wavesSurvived;
    private int aliveEnemies = 0;
    private TowerUnit tower;

    [Header("Formation Settings")]
    public float enemySpacing = 1f;

    [Header("UI References")]
    public TextMeshProUGUI waveMessageText;
    public float firstWaveDelay = 10f;

    [Header("Dependencies")]
    private GridManager gridManager;
    private ProceduralMusicGenerator musicGenerator;
    private ProceduralPercussionGenerator percussionGenerator;

    IEnumerator Start()
    {
        gridManager = FindFirstObjectByType<GridManager>();
        musicGenerator = FindFirstObjectByType<ProceduralMusicGenerator>();
        percussionGenerator = FindFirstObjectByType<ProceduralPercussionGenerator>();

        while (gridManager.enemyPaths.Count == 0)
            yield return null;

        if (waveMessageText != null)
        {
            waveMessageText.text = "Place your defenders!";
            waveMessageText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(firstWaveDelay);

        StartCoroutine(SpawnNextProceduralWave());
    }

    IEnumerator SpawnNextProceduralWave()
    {
        // Wait until all enemies are dead before spawning the next wave
        while (aliveEnemies > 0)
        {
            yield return null;
        }

        // Skip evaluation for first wave (wave index 0)
        if (currentWaveIndex > 0)
        {
            float playerDominated = EvaluatePlayerPerformance();

            currentDNA.Mutate(mutationRate, playerDominated);
        }

        if (waveMessageText != null)
        {
            waveMessageText.text = $"Wave {CurrentWave} is about to start!";
            waveMessageText.gameObject.SetActive(true);
        }

        yield return new WaitForSeconds(4f);
        waveMessageText.gameObject.SetActive(false);
        
        // Update procedural music difficulty
        float progress = NormalizeWaveIndex(currentWaveIndex);
        float difficultyFactor = difficultyCurve.Evaluate(progress);
        int musicDifficulty = Mathf.Clamp(Mathf.RoundToInt(difficultyFactor * 5f), 1, 5);

        if (musicGenerator != null)
            musicGenerator.SetDifficulty(musicDifficulty);
        
        if (percussionGenerator != null)
            percussionGenerator.SetDifficulty(musicDifficulty);

        EnemyWave wave = GenerateWave(currentWaveIndex);

        yield return StartCoroutine(SpawnWave(wave));
        
        currentWaveIndex++;

        StartCoroutine(SpawnNextProceduralWave());
    }

    EnemyWave GenerateWave(int waveIndex)
    {
        EnemyWave wave = new EnemyWave();
        wave.waveName = $"Wave {waveIndex + 1}";

        float progress = NormalizeWaveIndex(waveIndex);
        float difficultyFactor = difficultyCurve.Evaluate(progress);
        int budget = Mathf.RoundToInt(baseWaveBudget * difficultyFactor * currentDNA.aggressiveness);
        
        List<(EnemyType type, float dynamicWeight)> weightedEnemies = new List<(EnemyType, float)>();

        float playerPerformance = EvaluatePlayerPerformance();

        foreach (var enemy in enemyTypes)
        {
            float baseWeight = 1f;
            float tierWeight = 1f;
            
            float difficultyNormalized = Mathf.InverseLerp(0.5f, 1.5f, difficultyFactor);

            switch (enemy.tier)
            {
                case EnemyTier.Easy:
                    tierWeight = Mathf.Lerp(5f, 0.5f, difficultyNormalized); // Easy gets less likely over time
                    break;
                case EnemyTier.Medium:
                    tierWeight = Mathf.Lerp(1f, 2.5f, difficultyNormalized); // Medium becomes more likely
                    break;
                case EnemyTier.Hard:
                    tierWeight = Mathf.Lerp(0.2f, 5f, difficultyNormalized); // Hard becomes much more likely
                    break;
            }

            // Apply performance modifier
            float performanceModifier = Mathf.Clamp01(1f - (playerPerformance * 0.5f));
            baseWeight = tierWeight * performanceModifier;

            weightedEnemies.Add((enemy, baseWeight));
        }
        
        // Get minimum enemy cost
        int minEnemyCost = int.MaxValue;
        foreach (var enemy in enemyTypes)
        {
            if (enemy.cost < minEnemyCost)
                minEnemyCost = enemy.cost;
        }
        
        int safetyLimit = 100;
        int attempts = 0;

        while (budget >= minEnemyCost && attempts < safetyLimit)
        {
            EnemyType chosen = GetWeightedRandomEnemy(weightedEnemies);
            
            int count = Mathf.RoundToInt(Random.Range(1, 3) * currentDNA.aggressiveness);
            int totalCost = chosen.cost * count;

            if (totalCost > budget)
            {
                attempts++;
                continue;
            }
            
            float interval = chosen.spawnInterval / currentDNA.spawnDensity;

            EnemyGroup group = new EnemyGroup
            {
                enemyPrefab = chosen.prefab,
                count = count,
                spawnInterval = interval
            };

            wave.enemyGroups.Add(group);
            
            budget -= chosen.cost * count;
        }

        wave.timeBeforeNextWave = 5f;
        return wave;
    }

    EnemyType GetWeightedRandomEnemy(List<(EnemyType type, float dynamicWeight)> weightedList)
    {
        float totalWeight = 0f;
        foreach (var e in weightedList)
            totalWeight += e.dynamicWeight;

        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (var e in weightedList)
        {
            cumulative += e.dynamicWeight;
            if (rand <= cumulative)
            {
                return e.type;
            }
        }
        
        return weightedList[0].type;
    }

    IEnumerator SpawnWave(EnemyWave wave)
    {
        foreach (EnemyGroup group in wave.enemyGroups)
        {
            if (gridManager.enemyPaths.Count == 0) yield break;

            int pathIndex = Random.Range(0, gridManager.enemyPaths.Count);
            List<Vector2Int> path = gridManager.enemyPaths[pathIndex];
            BoolWrapper groupStopFlag = new BoolWrapper();

            for (int i = 0; i < group.count; i++)
            {
                SpawnEnemy(group.enemyPrefab, path, i, groupStopFlag);
                yield return new WaitForSeconds(group.spawnInterval);
            }

            yield return new WaitForSeconds(2f);
        }

        yield return new WaitForSeconds(wave.timeBeforeNextWave);

        // Notify defenders
        DefenderUnit[] allDefenders = FindObjectsByType<DefenderUnit>(FindObjectsSortMode.None);
        foreach (var defender in allDefenders)
        {
            defender.OnWaveCompleted();
        }
        
        bool towerSurvived = tower != null && tower.IsAlive;
        if (towerSurvived)
        {
            wavesSurvived++;
        }
    }

    void SpawnEnemy(GameObject enemyPrefab, List<Vector2Int> path, int columnIndex, BoolWrapper groupStopFlag)
    {
        Vector3 spawnPos = gridManager.GridToWorld(path[0]);
        spawnPos += new Vector3(columnIndex * enemySpacing, 0, 0);

        GameObject enemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        enemy.GetComponent<EnemyMovement>().InitPath(path, gridManager, columnIndex * enemySpacing, groupStopFlag);

        aliveEnemies++;
    }

    float EvaluatePlayerPerformance()
    {
        if (tower == null)
        {
            tower = FindFirstObjectByType<TowerUnit>();
            if (tower == null)
            {
                return -1f; // worst-case
            }
        }

        float healthRatio = Mathf.Clamp01(tower.currentHealth / tower.maxHealth);

        // Scale to [-1, 1]
        float score = (healthRatio - 0.5f) * 2f;
        
        return score;
    }
    
    float NormalizeWaveIndex(int waveIndex)
    {
        // Grows slower over time: ~0.5 at wave 10, ~0.8 at wave 30
        return Mathf.Clamp01(Mathf.Log10(waveIndex + 1) / 2f);
    }
    
    void OnEnable()
    {
        EnemyUnit.OnEnemyDied += HandleEnemyDeath;
    }

    void OnDisable()
    {
        EnemyUnit.OnEnemyDied -= HandleEnemyDeath;
    }

    void HandleEnemyDeath(int reward)
    {
        aliveEnemies--;
    }
}
