using UnityEngine;
using UnityEngine.UI;

public class DefenderRotationUI : MonoBehaviour
{
    public static DefenderRotationUI Instance;

    [Header("UI References")]
    public GameObject panel;
    public Button rotateLeftButton;
    public Button rotateRightButton;
    public Button destroyButton;
    public Button placeButton;

    private GameObject currentDefender;
    private DefenderSpot activeSpot;

    private void Awake()
    {
        Instance = this;
        Hide();

        rotateLeftButton.onClick.AddListener(() => RotateDefender(-90f));
        rotateRightButton.onClick.AddListener(() => RotateDefender(90f));
        destroyButton.onClick.AddListener(DestroyDefender);
        placeButton.onClick.AddListener(PlaceDefender);
    }

    public void Show(GameObject defender, DefenderSpot spot)
    {
        currentDefender = defender;
        activeSpot = spot;
        panel.SetActive(true);
    }

    public void Hide()
    {
        panel.SetActive(false);
        currentDefender = null;
        activeSpot = null;
    }

    private void RotateDefender(float angle)
    {
        if (currentDefender != null)
        {
            currentDefender.transform.Rotate(Vector3.up, angle);
        }
    }

    private void DestroyDefender()
    {
        if (activeSpot != null)
        {
            activeSpot.DestroyDefender();
        }
        Hide();
    }

    private void PlaceDefender()
    {
        if (currentDefender != null)
        {
            Hide();
        }
    }
}
