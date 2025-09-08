using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public abstract class AttackableUnit : MonoBehaviour
{
    [Header("Unit Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public Image healthBar;
    public Color flashColour = Color.red;
    public float flashDuration = 0.2f;

    public bool IsAlive => currentHealth > 0;

    private Color originalColour;

    protected virtual void Start()
    {
        currentHealth = maxHealth;

        if (healthBar != null)
        {
            healthBar.fillAmount = 1f;
            originalColour = healthBar.color;
        }
    }

    public virtual void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);

        UpdateHealthBar();

        if (healthBar != null)
            StartCoroutine(FlashHealthBar());

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = currentHealth / maxHealth;
        }
    }

    private IEnumerator FlashHealthBar()
    {
        if (healthBar == null) yield break;

        // Flash to flashColor
        healthBar.color = flashColour;

        yield return new WaitForSeconds(flashDuration);

        // Return to original color
        healthBar.color = originalColour;
    }

    protected virtual void Die()
    {
        Destroy(gameObject);
    }

    protected abstract void Attack();
}
