using System.Collections;
using UnityEngine;

public class TowerUnit : AttackableUnit, IUpgradeableUnit
{
    [Header("Tower Settings")]
    public float damage = 20f;
    public float range = 8f;
    public float fireRate = 1f;
    public LayerMask enemyLayer;

    [Header("Laser Settings")]
    [SerializeField] private LineRenderer laserRenderer;
    public ParticleSystem impactEffect;
    public Transform firePoint;
    
    [Header("Smoke Effect (Full-Screen Shader)")]
    public float maxSmokeIntensity = 1f;
    public float smokeLerpSpeed = 2f;
    
    [Header("Upgrade Settings")]
    public int upgradeLevel = 0;
    public int maxUpgrades = 2;
    public int baseUpgradeCost = 50;
    public float healthUpgradeMultiplier = 1.25f;
    public float damageUpgradeMultiplier = 1.2f;
    [SerializeField] private GameObject[] upgradeMeshes;

    private float fireCooldown;
    private Collider currentTarget;
    
    public float MaxHealth => maxHealth;

    protected virtual void Awake()
    {
        // Update mesh
        UpdateMeshForUpgrade();
    }

    private void Update()
    {
        if (!IsAlive) return;

        fireCooldown -= Time.deltaTime;

        // Acquire target if needed
        if (currentTarget == null || 
            !currentTarget.GetComponentInParent<AttackableUnit>()?.IsAlive == true || 
            Vector3.Distance(transform.position, currentTarget.transform.position) > range)
        {
            currentTarget = GetFirstEnemyInRange();
        }

        if (currentTarget != null && fireCooldown <= 0f)
        {
            RotateToTarget(currentTarget.transform);
            Attack();
            fireCooldown = 1f / fireRate;
        }
        
        if (SmokeEffectController.Instance == null || SmokeEffectController.Instance.smokeMaterial == null)
            return;

        float healthPercent = currentHealth / maxHealth;

        float baseIntensity = 0.5f;
        float intensityMultiplier = 10f;
        float damageFactor = Mathf.Pow(1f - healthPercent, 2f);

        float targetIntensity = Mathf.Clamp(baseIntensity + damageFactor * intensityMultiplier, 0f, maxSmokeIntensity);

        // Get current intensity
        Material mat = SmokeEffectController.Instance.smokeMaterial;
        float currentIntensity = mat.GetFloat("_VignetteIntensity");

        // Apply smooth transition
        float newIntensity = Mathf.MoveTowards(currentIntensity, targetIntensity, Time.deltaTime * smokeLerpSpeed * 10f);
        mat.SetFloat("_VignetteIntensity", newIntensity);
    }

    protected override void Attack()
    {
        if (currentTarget == null) return;

        AttackableUnit enemyUnit = currentTarget.GetComponentInParent<AttackableUnit>();
        if (enemyUnit != null)
        {
            FireLaser(enemyUnit, damage);
        }
    }

    private Collider GetFirstEnemyInRange()
    {
        Collider[] enemies = Physics.OverlapSphere(transform.position, range, enemyLayer);
        if (enemies.Length == 0) return null;

        return enemies[0]; // first detected enemy
    }

    private void RotateToTarget(Transform target)
    {
        Vector3 dir = (target.position - firePoint.position).normalized;
        Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
        firePoint.rotation = Quaternion.Lerp(firePoint.rotation, lookRot, Time.deltaTime * 10f);
    }

    private void FireLaser(AttackableUnit target, float laserDamage)
    {
        if (firePoint == null || target == null) return;

        // Apply damage
        target.TakeDamage(laserDamage);

        // Draw quick visible laser
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

    protected override void Die()
    {
        GameManager.Instance.ShowGameOver();
        Destroy(gameObject);
    }
    
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
    
    private void UpdateMeshForUpgrade()
    {
        if (upgradeMeshes == null || upgradeMeshes.Length == 0)
            return;

        for (int i = 0; i < upgradeMeshes.Length; i++)
        {
            upgradeMeshes[i].SetActive(i == upgradeLevel); // only show the current upgrade level
        }
    }
}
