
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SampleEnemy : MonoBehaviour, IDamageable
{
    [Header("Health")]
    public float maxHealth = 50f;
    public bool destroyOnDeath = true;

    [Header("VFX (optional)")]
    public GameObject hitEffect;   // small spawn at hit point (optional)
    public GameObject deathEffect; // spawn on death (optional)

    float currentHealth;

    void Awake()
    {
        currentHealth = Mathf.Max(0.01f, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (currentHealth <= 0f) return;

        currentHealth -= Mathf.Max(0f, amount);

        // optional small hit feedback
        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        // spawn death VFX
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // disable interactions first (prevent multiple hits)
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        // optional: play death animation / sound here

        if (destroyOnDeath)
            Destroy(gameObject);
    }

    // Optional helper for external checks
    public bool IsAlive => currentHealth > 0f;

    // Editor convenience: show health in inspector at runtime
#if UNITY_EDITOR
    void OnValidate()
    {
        if (maxHealth < 0f) maxHealth = 0f;
    }
#endif
}