using UnityEngine;

public abstract class AttackableUnit : MonoBehaviour
{
    [Header("Unit Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    public bool IsAlive => currentHealth > 0;

    protected virtual void Start()
    {
        currentHealth = maxHealth;
    }

    // Shared health logic
    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    protected virtual void Die()
    {
        // Default death behaviour, can be overridden
        Destroy(gameObject);
    }

    // Attack is abstract — each unit defines its own
    protected abstract void Attack();
}
