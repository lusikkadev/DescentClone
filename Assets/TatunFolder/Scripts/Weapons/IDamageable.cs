
using UnityEngine;

public interface IDamageable
{
    /// <summary>
    /// Apply damage to this object. Implementations should handle death when health reaches zero.
    /// </summary>
    /// <param name="amount">Damage amount (positive = damage).</param>
    void TakeDamage(float amount);
}