using UnityEngine;

public class ResistorDefender : DefenderUnit
{
    [Header("Resistor Settings")]
    public float fireCooldown = 1.5f;
    private float fireTimer = 0f;

    public float slowAmount = 0.5f;     // 50% speed
    public float slowDuration = 2f;     // seconds
    public float stunDuration = 1f;     // overclock only
    
    private int lastTargetIndex = -1;

    protected override void Update()
    {
        base.Update();

        fireTimer -= Time.deltaTime;

        if (fireTimer <= 0f)
        {
            AttackableUnit target = GetNextEnemyInRange();
            if (target != null)
            {
                Attack(target);
                fireTimer = fireCooldown;
            }
        }
    }

    private void Attack(AttackableUnit target)
    {
        FireLaser(target, 0f);

        // Apply slow + stun if needed
        EnemyMovement movement = target.GetComponentInParent<EnemyMovement>();
        if (movement != null)
        {
            movement.ApplySlow(slowAmount, slowDuration);

            if (isOverclocked)
            {
                movement.Stun(stunDuration);
            }
        }
    }

    protected override void OnOverclockStart()
    {
        // Optional burst: can instantly fire
        AttackableUnit target = GetNextEnemyInRange();
        if (target != null)
        {
            Attack(target);
        }
    }
    
    private AttackableUnit GetNextEnemyInRange()
    {
        Collider[] enemies = GetEnemiesInRange();
        if (enemies.Length == 0) return null;

        int startIndex = (lastTargetIndex + 1) % enemies.Length;

        for (int i = 0; i < enemies.Length; i++)
        {
            int currentIndex = (startIndex + i) % enemies.Length;
            var enemy = enemies[currentIndex].GetComponentInParent<AttackableUnit>();
            if (enemy != null && enemy.IsAlive)
            {
                lastTargetIndex = currentIndex;
                return enemy;
            }
        }

        return null;
    }
}
