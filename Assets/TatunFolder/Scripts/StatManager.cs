using UnityEngine;

public class StatManager : MonoBehaviour, IDamageable
{
    // Player Stats
    public int maxHealth = 100;
    public int health = 100;
    public int maxEnergy = 100;
    public int energy = 100;

    

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health < 0) health = 0;
        {
            Die();
        }
    }

    public void Die()
    {
        // all player death logic here

    }
}
