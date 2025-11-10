using UnityEngine;

public class SmokeEffectController : MonoBehaviour
{
    public static SmokeEffectController Instance { get; private set; }
    
    public Material smokeMaterial;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }
}
