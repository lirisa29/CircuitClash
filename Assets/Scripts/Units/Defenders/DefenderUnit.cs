using System.Collections;
using TMPro;
using UnityEngine;

public class DefenderUnit : AttackableUnit, IUpgradeableUnit
{
    [Header("Defender Settings")]
    public float damage = 20f;
    public float range = 5f;
    public float attackAngle = 180f;
    public LayerMask enemyLayer;
    public DefenderData data;

    [Header("Laser Settings")]
    [SerializeField] private LineRenderer laserRenderer;
    public ParticleSystem impactEffect;
    public Transform firePoint;

    [Header("Overclock Settings")]
    public bool isOverclocked = false;
    public float overclockDuration = 10f;
    private float overclockTimer;
    
    [Header("Upgrade Settings")]
    public int upgradeLevel = 0;
    public int maxUpgrades = 2;
    public int baseUpgradeCost = 50;
    public float healthUpgradeMultiplier = 1.25f;
    public float damageUpgradeMultiplier = 1.2f;
    [SerializeField] private GameObject[] upgradeMeshes;
    
    private int wavesSurvivedAlive = 0; 
    private MeshRenderer[] meshRenderers;
    private Color originalColor;
    public Color overclockGlowColor = Color.cyan;
    
    public float MaxHealth => maxHealth;
    
    protected virtual void Awake()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        if (meshRenderers.Length > 0)
            originalColor = meshRenderers[0].material.color;
        
        // Update mesh
        UpdateMeshForUpgrade();
    }

    protected virtual void Update()
    {
        if (isOverclocked)
        {
            overclockTimer -= Time.deltaTime;
            if (overclockTimer <= 0f)
            {
                isOverclocked = false;
                OnOverclockEnd();
            }
        }
    }
    
    public void OnWaveCompleted()
    {
        wavesSurvivedAlive++;
        if (wavesSurvivedAlive % 5 == 0)
        {
            ActivateOverclock();
        }
    }

    public void ActivateOverclock()
    {
        isOverclocked = true;
        overclockTimer = overclockDuration;
        OnOverclockStart();

        // Glow effect
        foreach (var mr in meshRenderers)
        {
            mr.material.SetColor("_EmissionColor", overclockGlowColor * 2f);
            mr.material.EnableKeyword("_EMISSION");
        }
    }

    protected virtual void OnOverclockStart() { }

    protected virtual void OnOverclockEnd()
    {
        // Reset glow
        foreach (var mr in meshRenderers)
        {
            mr.material.SetColor("_EmissionColor", Color.black);
            mr.material.DisableKeyword("_EMISSION");
            mr.material.color = originalColor;
        }
    }

    // Helpers
    protected Collider[] GetEnemiesInRange()
    {
        Collider[] allEnemies = Physics.OverlapSphere(transform.position, range, enemyLayer);

        // Filter by angle
        var filtered = new System.Collections.Generic.List<Collider>();
        foreach (var enemy in allEnemies)
        {
            Vector3 dirToEnemy = (enemy.transform.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, dirToEnemy);

            if (angle <= attackAngle * 0.5f) // inside arc
            {
                filtered.Add(enemy);
            }
        }

        return filtered.ToArray();
    }

    protected Collider GetFirstEnemyInRange()
    {
        Collider[] enemies = GetEnemiesInRange();
        if (enemies.Length == 0) return null;

        Collider firstEnemy = null;
        int bestProgress = -1;

        foreach (var enemy in enemies)
        {
            EnemyMovement movement = enemy.GetComponentInParent<EnemyMovement>();
            if (movement != null)
            {
                if (movement.CurrentIndex > bestProgress)
                {
                    bestProgress = movement.CurrentIndex;
                    firstEnemy = enemy;
                }
            }
        }

        return firstEnemy;
    }

    // Spawns Laser
    protected void FireLaser(AttackableUnit target, float laserDamage)
    {
        if (firePoint == null || target == null) return;

        // Apply damage
        target.TakeDamage(laserDamage);

        // Draw visible laser
        if (laserRenderer != null)
        {
            RaycastHit hit;
            Vector3 dir = (target.transform.position - firePoint.position).normalized;
            Vector3 hitPos = target.transform.position;

            if (Physics.Raycast(firePoint.position, dir, out hit, range, enemyLayer))
            {
                hitPos = hit.point; // surface position
            }

            impactEffect.transform.position = hitPos;
            
            StartCoroutine(LaserEffect(target.transform.position));
        }
    }
    
    private IEnumerator LaserEffect(Vector3 hitPos)
    {
        laserRenderer.enabled = true;
        impactEffect.Play();
        
        laserRenderer.SetPosition(0, firePoint.position);
        laserRenderer.SetPosition(1, hitPos);

        yield return new WaitForSeconds(0.1f); // flash duration

        laserRenderer.enabled = false;
    }

    protected override void Attack() { }
    
    public int GetUpgradeCost()
    {
        return baseUpgradeCost * (upgradeLevel + 1);
    }
    
    public bool CanUpgrade()
    {
        return upgradeLevel < maxUpgrades;
    }
    
    public virtual void Upgrade()
    {
        if (!CanUpgrade()) return;
        
        upgradeLevel++;
        
        // Upgrade stats
        maxHealth *= healthUpgradeMultiplier;
        currentHealth = maxHealth;
        damage *= damageUpgradeMultiplier;

        UpdateHealthBar();
        
        // Update mesh
        UpdateMeshForUpgrade();
    }
    
    public virtual string GetSecondaryStatText()
    {
        return $"Damage: {damage:F0}";
    }
    
    public void UpdateMeshForUpgrade()
    {
        if (upgradeMeshes == null || upgradeMeshes.Length == 0)
            return;

        for (int i = 0; i < upgradeMeshes.Length; i++)
        {
            upgradeMeshes[i].SetActive(i == upgradeLevel); // only show the current upgrade level
        }
    }
}
