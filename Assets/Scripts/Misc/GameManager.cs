using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("All Defenders")] 
    public DefenderData[] allDefenders;
    
    [Header("Dependencies")]
    public EnemySpawner enemySpawner;

    private void Awake()
    {
        Instance = this;
    }
}
