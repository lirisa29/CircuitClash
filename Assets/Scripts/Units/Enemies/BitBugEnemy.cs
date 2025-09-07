using UnityEngine;

public class BitBugEnemy : EnemyUnit
{
    [Header("BitBug Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;

    protected override void Attack()
    {
        Collider target = GetFirstDefenderInRange();
        
        if (target != null)
        {
            // Damage first defender
            AttackableUnit mainTarget = target.GetComponent<AttackableUnit>();
            if (mainTarget != null)
            {
                // Spawn projectile
                GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                Projectile proj = projObj.GetComponent<Projectile>();
                proj.Initialize(mainTarget, damage);
            }
        }
    }
}
