using UnityEngine;
using UnityEngine.SceneManagement;

public class StatManager : MonoBehaviour, IDamageable
{
    UIManager uiManager;


    // Player Stats
    public int maxHealth = 100;
    public int health = 100;
    public int maxEnergy = 100;
    public int energy = 100;

    // Cooldowns for energy and health regen TO BE IMPLEMENTED
    public int minEnergy = 10;
    public float energyRegenCooldown = 2f;
    public float healthRegenCooldown = 5f;

    private void Awake()
    {
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        uiManager.UpdateHealthBar();
        if (health < 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        health += amount;
        uiManager.UpdateHealthBar();
        if (health > maxHealth) health = maxHealth;
    }

    public void UseEnergy(int amount)
    {
        energy -= amount;
        uiManager.UpdateEnergyBar();
        if (energy < 0) energy = 0;
    }

    public void RegainEnergy(int amount)
    {
        energy += amount;
        uiManager.UpdateEnergyBar();
        if (energy > maxEnergy) energy = maxEnergy;
    }

    public void Die()
    {
        // all player death logic here
        SceneManager.LoadScene(0);
    }
}
