using System.Collections.Generic;
using UnityEngine;

public class InductorDefender : DefenderUnit
{
    [Header("Inductor Settings")]
    public float fireCooldown = 2f;
    private float fireTimer = 0f;

    public int baseBounceCount = 3;
    public int extraOverclockBounces = 2;
    public float bounceRange = 3f;
    public float damageFalloff = 0.8f; // each bounce deals 80% of previous
    
    private int lastTargetIndex = -1;

    protected override void Update()
    {
        base.Update();

        fireTimer -= Time.deltaTime;
        if (fireTimer <= 0f)
        {
            AttackableUnit firstTarget = GetNextEnemyInRange();
            if (firstTarget != null)
            {
                Attack(firstTarget);
                fireTimer = fireCooldown;
            }
        }
    }

    protected override void Attack()
    {
        // not used
    }

    private void Attack(AttackableUnit firstTarget)
    {
        int totalBounces = baseBounceCount + (isOverclocked ? extraOverclockBounces : 0);
        ChainLightning(firstTarget, totalBounces, damage, new HashSet<AttackableUnit>());
    }

    private void ChainLightning(AttackableUnit target, int remainingBounces, float currentDamage, HashSet<AttackableUnit> alreadyHit)
    {
        if (target == null || remainingBounces <= 0 || alreadyHit.Contains(target)) return;

        // Mark this target as hit
        alreadyHit.Add(target);

        // Deal damage
        target.TakeDamage(currentDamage);

        // Visual
        FireLaser(target, currentDamage);

        // Find next nearby enemy
        Collider[] hits = Physics.OverlapSphere(target.transform.position, bounceRange, enemyLayer);
        AttackableUnit nextTarget = null;

        foreach (Collider hit in hits)
        {
            var unit = hit.GetComponentInParent<AttackableUnit>();
            if (unit != null && unit.IsAlive && !alreadyHit.Contains(unit))
            {
                nextTarget = unit;
                break;
            }
        }

        if (nextTarget != null)
        {
            ChainLightning(nextTarget, remainingBounces - 1, currentDamage * damageFalloff, alreadyHit);
        }
    }

    protected override void OnOverclockStart()
    {
        // Optional: fire instantly
        var target = GetNextEnemyInRange();
        if (target != null)
        {
            Attack(target);
        }
    }

    private AttackableUnit GetNextEnemyInRange()
    {
        Collider[] enemies = GetEnemiesInRange();
        if (enemies.Length == 0)
        {
            lastTargetIndex = -1;
            return null;
        }

        int startIndex = (lastTargetIndex + 1) % enemies.Length;

        for (int i = 0; i < enemies.Length; i++)
        {
            int currentIndex = (startIndex + i) % enemies.Length;
            var unit = enemies[currentIndex].GetComponentInParent<AttackableUnit>();
            if (unit != null && unit.IsAlive)
            {
                lastTargetIndex = currentIndex;
                return unit;
            }
        }

        lastTargetIndex = -1;
        return null;
    }
}
