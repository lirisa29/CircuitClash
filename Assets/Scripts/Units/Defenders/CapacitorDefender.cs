using UnityEngine;

public class CapacitorDefender : DefenderUnit
{
    [Header("Capacitor Settings")]
    public float chargeTime = 3f;
    private float chargeTimer = 0f;

    public float shockwaveDamageMultiplier = 1.5f; // stronger in overclock

    protected override void Update()
    {
        base.Update();

        // Only charge if not overclocked
        if (!isOverclocked)
        {
            chargeTimer += Time.deltaTime;
            if (chargeTimer >= chargeTime)
            {
                Attack();
                chargeTimer = 0f;
            }
        }
        else
        {
            Attack();
        }
    }

    protected override void Attack()
    {
        if (isOverclocked)
        {
            // Overclock attack: wide shockwave hitting all enemies
            Collider[] enemies = GetEnemiesInRange();
            foreach (Collider enemy in enemies)
            {
                AttackableUnit unit = enemy.GetComponentInParent<AttackableUnit>();
                if (unit != null)
                {
                    FireLaser(unit, damage * shockwaveDamageMultiplier);
                }
            }
        }
        else
        {
            // Normal attack: find first enemy, apply damage
            Collider target = GetFirstEnemyInRange();
            if (target != null)
            {
                // Damage first enemy
                AttackableUnit mainTarget = target.GetComponentInParent<AttackableUnit>();
                if (mainTarget != null)
                    FireLaser(mainTarget, damage);
            }
        }
    }

    protected override void OnOverclockStart()
    {
        // Instantly fire shockwave once when overclock begins
        Attack();
    }
}
