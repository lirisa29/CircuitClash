using UnityEngine;
using UnityEngine.EventSystems;

public class TowerSpot : MonoBehaviour, IPointerClickHandler
{
    private bool statsOpen;
    
    public void OnPointerClick(PointerEventData eventData)
    {
        // Find the parent that has IUpgradeableUnit
        TowerUnit unit = GetComponentInParent<TowerUnit>();
        if (unit == null)
            return; // nothing to show

        if (!statsOpen)
        {
            TowerStatsUI.Instance.Show(unit.gameObject);
            statsOpen = true;
        }
        else
        {
            TowerStatsUI.Instance.Hide();
            statsOpen = false;
        }
    }
}
