using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    private AttackableUnit target;
    private float damage;

    public void Initialize(AttackableUnit targetUnit, float damageAmount)
    {
        target = targetUnit;
        damage = damageAmount;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.transform.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        if (Vector3.Distance(transform.position, target.transform.position) < 0.2f)
        {
            target.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
