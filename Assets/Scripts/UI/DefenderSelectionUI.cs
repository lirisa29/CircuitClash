using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class DefenderSelectionUI : MonoBehaviour
{
    public static DefenderSelectionUI Instance;

    [Header("UI References")]
    public GameObject defenderCardPrefab;   // prefab for each defender card
    public Transform cardParent;         // parent container for cards
    public GameObject panel;             // the full selection panel

    private DefenderSpot activeSpot;

    private void Awake()
    {
        Instance = this;
        Hide();
    }

    public void Show(DefenderData[] towers, DefenderSpot spot)
    {
        activeSpot = spot;

        // Clear old cards
        foreach (Transform child in cardParent)
        {
            Destroy(child.gameObject);
        }

        int currentWave = GameManager.Instance.enemySpawner.CurrentWave;

        // Create cards for each tower
        foreach (DefenderData tower in towers)
        {
            GameObject card = Instantiate(defenderCardPrefab, cardParent);

            card.transform.Find("Icon").GetComponent<Image>().sprite = tower.defenderSprite;
            card.transform.Find("Name").GetComponent<TextMeshProUGUI>().text = tower.defenderName;
            card.transform.Find("Cost").GetComponent<TextMeshProUGUI>().text = tower.voltageCost.ToString();
            card.transform.Find("Description").GetComponent<TextMeshProUGUI>().text = 
                $"{tower.description}\nEffect: {tower.effect}\nOverclock: {tower.overclockBonus}";

            Button btn = card.GetComponent<Button>();

            if (currentWave < tower.unlockedWave)
            {
                // Locked → disable interaction + gray out
                btn.interactable = false;
                card.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.3f);
            }
            else
            {
                // Unlocked → allow purchase
                btn.onClick.AddListener(() => OnTowerSelected(tower));
            }
        }

        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
        activeSpot = null;
    }

    private void OnTowerSelected(DefenderData tower)
    {
        PlayerResources player = FindFirstObjectByType<PlayerResources>();

        if (player.SpendVoltage(tower.voltageCost))
        {
            activeSpot.PlaceDefender(tower);
            Hide();
        }
        else
        {
            Debug.Log("Not enough voltage to place tower.");
        }
    }
}
