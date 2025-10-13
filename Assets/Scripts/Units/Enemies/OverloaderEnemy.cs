using UnityEngine;

public class OverloaderEnemy : EnemyUnit
{
    [Header("Overloader Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    
    public float explosionRadius = 2.5f;
    public float explosionDamage = 30f;

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
        // Explosion on death before actual destroy
        ExplodeOnDeath();

        // Give reward + destroy
        base.Die();
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

        // Optional: Add explosion VFX or sound
        // Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
    }
}
