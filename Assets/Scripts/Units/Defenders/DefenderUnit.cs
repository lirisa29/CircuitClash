using System.Collections;
using UnityEngine;

public class DefenderUnit : AttackableUnit
{
    [Header("Defender Settings")]
    public float damage = 20f;
    public float range = 5f;
    public float attackAngle = 180f;
    public LayerMask enemyLayer;
    public DefenderData data;

    [Header("Laser Settings")]
    [SerializeField] private LineRenderer laserRenderer;
    public Transform firePoint;

    [Header("Overclock Settings")]
    public bool isOverclocked = false;
    public float overclockDuration = 10f;
    private float overclockTimer;

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

    public void ActivateOverclock()
    {
        isOverclocked = true;
        overclockTimer = overclockDuration;
        OnOverclockStart();
    }

    protected virtual void OnOverclockStart() { }
    protected virtual void OnOverclockEnd() { }

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
            EnemyMovement movement = enemy.GetComponent<EnemyMovement>();
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

    protected override void Attack() { }

    private void OnDrawGizmosSelected()
    {
        // Debugging: visualize attack range + angle
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);

        // Draw forward direction
        Vector3 forward = transform.forward * range;
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + forward);

        // Draw angle lines
        Quaternion leftRot = Quaternion.AngleAxis(-attackAngle * 0.5f, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(attackAngle * 0.5f, Vector3.up);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + leftRot * forward);
        Gizmos.DrawLine(transform.position, transform.position + rightRot * forward);
    }
}
