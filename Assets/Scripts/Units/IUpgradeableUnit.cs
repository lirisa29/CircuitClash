public interface IUpgradeableUnit
{
    float MaxHealth { get; }
    string GetSecondaryStatText();
    bool CanUpgrade();
    int GetUpgradeCost();
    void Upgrade();
}
