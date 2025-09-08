using System;
using UnityEngine;

public class EnemyUnit : AttackableUnit
{
    [Header("Enemy Settings")]
    public float damage = 5f;
    public float range = 5f;
    public float attackCooldown = 1.5f;
    public LayerMask defenderLayer;
    public LayerMask towerLayer;
    
    [Header("Reward")]
    public int voltageReward = 10; // set per enemy type
    
    protected float attackTimer = 0f;
    
    public static event Action<int> OnEnemyDied;
    
    protected EnemyMovement movement;

    protected override void Start()
    {
        base.Start(); // keep the health setup
        movement = GetComponent<EnemyMovement>();
    }

    protected virtual void Update()
    {
        attackTimer -= Time.deltaTime;
        
        // Attack defender if in range
        if (attackTimer <= 0f)
        {
            Attack();
            attackTimer = attackCooldown;
        }
    }
    
    protected Collider CheckIfTowerInRange()
    {
        Collider[] towers = Physics.OverlapSphere(transform.position, range, towerLayer);
        if (towers.Length == 0) return null;

        return towers[0];
    }

    // Helpers
    protected Collider[] GetDefendersInRange()
    {
        return Physics.OverlapSphere(transform.position, range, defenderLayer);
    }

    protected Collider GetFirstDefenderInRange()
    {
        Collider[] defenders = GetDefendersInRange();
        if (defenders.Length == 0) return null;

        System.Array.Sort(defenders, (a, b) =>
            Vector3.Distance(transform.position, a.transform.position)
                .CompareTo(Vector3.Distance(transform.position, b.transform.position)));

        return defenders[0];
    }
    
    protected override void Die()
    {
        // Reward player
        PlayerResources playerResources = FindFirstObjectByType<PlayerResources>();
        if (playerResources != null)
        {
            playerResources.RefundVoltage(voltageReward);
        }

        // Broadcast death event with reward
        OnEnemyDied?.Invoke(voltageReward);

        Destroy(gameObject);
    }
    
    protected override void Attack() { }
}
