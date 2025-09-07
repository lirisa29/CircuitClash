using TMPro;
using UnityEngine;

public class PlayerResources : MonoBehaviour
{
    [Header("Player Voltage Settings")]
    public int startingVoltage = 100;
    private int currentVoltage;

    [Header("UI Reference")]
    public TextMeshProUGUI voltageText; // Drag your UI text element here

    private void Awake()
    {
        currentVoltage = startingVoltage;
        UpdateUI();
    }
    
    // Checks if the player has enough voltage to spend.
    public bool CanAfford(int cost)
    {
        return currentVoltage >= cost;
    }
    
    // Deducts voltage if affordable. Returns true if successful.
    public bool SpendVoltage(int cost)
    {
        if (CanAfford(cost))
        {
            currentVoltage -= cost;
            UpdateUI();
            return true;
        }
        return false;
    }
    
    // Refunds voltage (e.g., when destroying a tower).
    public void RefundVoltage(int amount)
    {
        currentVoltage += amount;
        UpdateUI();
    }
    
    // Gets current voltage.
    public int GetVoltage()
    {
        return currentVoltage;
    }

    private void UpdateUI()
    {
        if (voltageText != null)
        {
            voltageText.text = "Voltage: " + currentVoltage;
        }
    }
}
