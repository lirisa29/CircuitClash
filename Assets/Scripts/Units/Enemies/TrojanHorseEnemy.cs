using UnityEngine;

public class TrojanHorseEnemy : EnemyUnit
{
    [Header("Trojan Settings")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    
    public bool startDisguised = true;
    private bool isDisguised = false;
    private bool hasRevealed = false;

    [Header("Disguise Visuals")]
    public GameObject disguisedModel;
    public GameObject trueModel;

    [Header("Stat Tuning")]
    public float disguisedDamage = 5f;
    public float revealedDamage = 25f;
    public float revealedSpeed = 1.8f;
    
    protected override void Start()
    {
        base.Start();
        isDisguised = startDisguised;
        hasRevealed = !startDisguised; // if we don't start disguised, consider revealed already

        // Ensure models' active states are correct
        if (disguisedModel != null) disguisedModel.SetActive(isDisguised);
        if (trueModel != null) trueModel.SetActive(!isDisguised);

        // Set initial damage according to disguise
        damage = isDisguised ? disguisedDamage : revealedDamage;
    }
    
    public override void TakeDamage(float dmg)
    {
        // If we are disguised and receive any damage > 0, reveal first.
        if (!hasRevealed && dmg > 0f)
        {
            Reveal();
        }

        // Now apply damage normally (calls AttackableUnit.TakeDamage)
        base.TakeDamage(dmg);
    }
    
    // Reveal into true form: swap visuals, update stats
    private void Reveal()
    {
        hasRevealed = true;
        isDisguised = false;

        // Swap models
        if (disguisedModel != null) disguisedModel.SetActive(false);
        if (trueModel != null) trueModel.SetActive(true);

        // Update stats
        damage = revealedDamage;

        if (movement != null)
        {
            movement.SetBaseSpeed(revealedSpeed);
        }
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
                // Shoot projectile at the target
                if (projectilePrefab != null && firePoint != null)
                {
                    GameObject projObj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                    Projectile proj = projObj.GetComponent<Projectile>();
                    if (proj != null)
                    {
                        proj.Initialize(mainTarget, damage);
                    }
                }
                else
                {
                    // Fallback: apply damage directly
                    mainTarget.TakeDamage(damage);
                }
            }
        }
    }
}
