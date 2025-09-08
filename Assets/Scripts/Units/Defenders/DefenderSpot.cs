using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

public class DefenderSpot : MonoBehaviour, IPointerClickHandler
{
    public Vector2Int gridPos;
    public GameObject currentDefender;
    private DefenderData[] availableDefenders;
    private GameObject defenderPreview;

    public void Initialise(Vector2Int pos, DefenderData[] defenders)
    {
        gridPos = pos;
        availableDefenders = defenders;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (currentDefender == null)
        {
            DefenderRotationUI.Instance.Hide();
            // Show selection menu with defenders for this spot
            DefenderSelectionUI.Instance.Show(availableDefenders, this);
        }
        else
        {
            DefenderSelectionUI.Instance.Hide();
            // Show rotation menu for the placed defender
            DefenderRotationUI.Instance.Show(currentDefender, this);
        }
    }

    public void PlaceDefender(DefenderData data)
    {
        if (defenderPreview != null) Destroy(defenderPreview);

        // Offset the defender's Y-position by +1.5
        Vector3 spawnPos = new Vector3(transform.position.x, transform.position.y + 1.5f, transform.position.z);

        // Spawn the defender prefab
        defenderPreview = Instantiate(data.defenderPrefab, spawnPos, Quaternion.identity);
        currentDefender = defenderPreview;

        DefenderRotationUI.Instance.Show(currentDefender, this);
    }

    public void DestroyDefender()
    {
        if (currentDefender != null)
        {
            PlayerResources player = FindFirstObjectByType<PlayerResources>();
            DefenderUnit defender = currentDefender.GetComponent<DefenderUnit>();
            if (defender != null)
            {
                // Refund 50% cost
                player.RefundVoltage(defender.data.voltageCost / 2);
            }

            Destroy(currentDefender);
            currentDefender = null;
        }
    }
}
