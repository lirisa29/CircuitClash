using UnityEngine;

public class OverloaderEnemy : EnemyUnit
{
    [Header("Overloader Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    
    [Header("Explosion Effect")]
    public float explosionSpeed = 0.1f;
    private Renderer targetRenderer;
    
    public float explosionRadius = 2.5f;
    public float explosionDamage = 30f;
    
    private float explosionProgress = 0f;
    private bool exploding = false;

    protected override void Start()
    {
        base.Start();

        // Find the Renderer in children
        targetRenderer = GetComponentInChildren<Renderer>();
    }

    protected override void Attack()
    {
        Collider target = CheckIfTowerInRange();

        if (target != null)
        {
            // Tower in range -> stop moving
            if (movement != null && movement.stopMovement != null)
                movement.stopMovement.value = true;
        }
        else
        {
            // No tower -> resume movement
            if (movement != null && movement.stopMovement != null)
                movement.stopMovement.value = false;

            // Try defenders instead
            target = GetFirstDefenderInRange();
        }

        if (target != null)
        {
            AttackableUnit mainTarget = target.GetComponentInParent<AttackableUnit>();
            if (mainTarget != null)
            {
                GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                Projectile proj = projObj.GetComponent<Projectile>();
                proj.Initialize(mainTarget, damage);
            }
        }
    }

    protected override void Die()
    {
        exploding = true;
        
        // Explosion on death before actual destroy
        ExplodeOnDeath();

        // Give reward
        base.Die();
    }
    
    protected override void Update()
    {
        base.Update();
        
        if (exploding && targetRenderer != null)
        {
            explosionProgress = Mathf.MoveTowards(explosionProgress, 1f, Time.deltaTime * explosionSpeed);

            targetRenderer.material.SetFloat("_ExplosionStrength", explosionProgress);

            if (explosionProgress >= 1f)
            {
                Destroy(gameObject); // cleanup after animation finishes
            }
        }
    }

    private void ExplodeOnDeath()
    {
        Collider[] nearbyTowers = Physics.OverlapSphere(transform.position, explosionRadius, towerLayer);

        foreach (var towerCollider in nearbyTowers)
        {
            AttackableUnit tower = towerCollider.GetComponent<AttackableUnit>();
            if (tower != null && tower.IsAlive)
            {
                tower.TakeDamage(explosionDamage);
            }
        }
    }
}
