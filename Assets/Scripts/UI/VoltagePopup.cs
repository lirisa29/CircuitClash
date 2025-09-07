using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class VoltagePopup : MonoBehaviour
{
    public float duration = 1f;
    public TextMeshProUGUI rewardText;
    private float timer;
    private bool isShowing;
    
    void OnEnable()
    {
        EnemyUnit.OnEnemyDied += HandleEnemyDied;
    }

    void OnDisable()
    {
        EnemyUnit.OnEnemyDied -= HandleEnemyDied;
    }
    
    private void HandleEnemyDied(int reward)
    {
        Setup("+" + reward + "V");
    }

    public void Setup(string text)
    {
        if (rewardText != null)
        {
            rewardText.text = text;
            rewardText.gameObject.SetActive(true);
        }

        timer = duration;
        isShowing = true;
    }

    void Update()
    {
        if (!isShowing) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            if (rewardText != null)
                rewardText.gameObject.SetActive(false);

            isShowing = false;
        }
    }
}
