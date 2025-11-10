using UnityEngine;

public class BitBugEnemy : EnemyUnit
{
    [Header("BitBug Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    protected override void Die()
    {
        base.Die();
        
        Destroy(gameObject);
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
}
