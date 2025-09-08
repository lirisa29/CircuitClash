using System.Collections;
using UnityEngine;

public class TowerUnit : AttackableUnit
{
    [Header("Tower Settings")]
    public float damage = 20f;
    public float range = 8f;
    public float fireRate = 1f;
    public LayerMask enemyLayer;

    [Header("Laser Settings")]
    [SerializeField] private LineRenderer laserRenderer;
    public Transform firePoint;

    private float fireCooldown;
    private Collider currentTarget;

    private void Update()
    {
        if (!IsAlive) return;

        fireCooldown -= Time.deltaTime;

        // Acquire target if needed
        if (currentTarget == null || !currentTarget.GetComponent<AttackableUnit>().IsAlive ||
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
    }

    protected override void Attack()
    {
        if (currentTarget == null) return;

        AttackableUnit enemyUnit = currentTarget.GetComponent<AttackableUnit>();
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
            StartCoroutine(LaserEffect(target.transform.position));
        }
    }

    private IEnumerator LaserEffect(Vector3 hitPos)
    {
        laserRenderer.enabled = true;
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
}
