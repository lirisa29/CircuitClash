using System.Collections;
using TMPro;
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
    
    private int wavesSurvivedAlive = 0; 
    private MeshRenderer[] meshRenderers;
    private Color originalColor;
    public Color overclockGlowColor = Color.cyan;
    
    protected virtual void Awake()
    {
        meshRenderers = GetComponentsInChildren<MeshRenderer>();
        if (meshRenderers.Length > 0)
            originalColor = meshRenderers[0].material.color;
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
}
