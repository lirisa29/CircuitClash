using UnityEngine;

public enum DefenderType
{
    Resistor,   // Slow tower
    Capacitor,  // Charge-shot splash
    Inductor    // Chain lightning
}

[CreateAssetMenu(fileName = "DefenderData", menuName = "Defender/DefenderData")]
public class DefenderData : ScriptableObject
{
    [Header("Basic Info")]
    public string defenderName;
    public DefenderType defenderType;
    public GameObject defenderPrefab;
    public Sprite defenderSprite;
    public int voltageCost;
    public int unlockedWave = 1;

    [Header("Description")]
    [TextArea(2, 4)]
    public string description;
    [TextArea(2, 4)] 
    public string effect;
    [TextArea(2, 4)] 
    public string overclockBonus;

    [Header("Stats (Optional)")]
    public int damage;
    public float attackSpeed;
    public float range;
    public float specialValue;
}
