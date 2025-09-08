using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("All Defenders")] 
    public DefenderData[] allDefenders;
    
    [Header("Dependencies")]
    public EnemySpawner enemySpawner;
    
    [Header("UI References")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI wavesSurvivedText;
    public Button restartButton;
    
    private int wavesSurvived;

    private void Awake()
    {
        Instance = this;
        
        // Hide GameOver panel initially
        gameOverPanel.SetActive(false);

        // Add listener for button
        restartButton.onClick.AddListener(OnRestartButtonClicked);
    }

    // Called when the tower dies
    public void ShowGameOver()
    {
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            wavesSurvived = enemySpawner.WavesSurvived;
        }
        
        wavesSurvivedText.text = "Waves Survived: " + wavesSurvived;
        
        gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void OnRestartButtonClicked()
    {
        Time.timeScale = 1f;
        // Reload current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
