using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TowerStatsUI : MonoBehaviour
{
    public static TowerStatsUI Instance;
    
    [Header("UI References")]
    public GameObject panel;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI damageText;
    public Button upgradeButton;
    public TextMeshProUGUI upgradeText;
    public TextMeshProUGUI notEnoughVoltageText;

    private IUpgradeableUnit currentUnit;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
        
        // Hide the "not enough voltage" text by default
        if (notEnoughVoltageText != null)
            notEnoughVoltageText.gameObject.SetActive(false);
    }

    public void Show(GameObject defender)
    {
        currentUnit = defender.GetComponent<IUpgradeableUnit>();
        panel.SetActive(true);
        UpdateStatsUI();
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentUnit = null;
    }

    private void OnUpgradeClicked()
    {
        if (currentUnit == null) return;

        PlayerResources player = FindFirstObjectByType<PlayerResources>();

        if (currentUnit.CanUpgrade())
        {
            int cost = currentUnit.GetUpgradeCost();
            if (player.SpendVoltage(cost))
            {
                currentUnit.Upgrade();
                UpdateStatsUI();
            }
            else
            {
                if (notEnoughVoltageText != null)
                {
                    notEnoughVoltageText.gameObject.SetActive(true);

                    // auto-hide after 2 seconds
                    CancelInvoke(nameof(HideNotEnoughVoltageText));
                    Invoke(nameof(HideNotEnoughVoltageText), 2f);
                }
            }
        }
    }

    public void UpdateStatsUI()
    {
        if (currentUnit == null) return;

        healthText.text = $"Health: {currentUnit.MaxHealth:F0}";
        damageText.text = currentUnit.GetSecondaryStatText();

        if (currentUnit.CanUpgrade())
        {
            upgradeButton.interactable = true;
            upgradeText.text = $"Upgrade: {currentUnit.GetUpgradeCost()}V";
        }
        else
        {
            upgradeButton.interactable = false;
            upgradeText.text = "MAXED";
        }
    }
    
    private void HideNotEnoughVoltageText()
    {
        if (notEnoughVoltageText != null)
            notEnoughVoltageText.gameObject.SetActive(false);
    }
}
