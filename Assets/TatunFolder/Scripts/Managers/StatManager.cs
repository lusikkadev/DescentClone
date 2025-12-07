using UnityEngine.SceneManagement;
using UnityEngine;
using System.Collections;

public class StatManager : MonoBehaviour, IDamageable
{
    public static StatManager Instance { get; private set; }
    UIManager uiManager;
    WeaponManager weaponManager;

    // Player Stats
    public int maxHealth = 100;
    public int health = 100;
    public int maxEnergy = 100;
    public float energy = 100;

    // Cooldowns / rates
    public int minEnergy = 10;
    //[SerializeField] float energyRegenCooldown = 5f; 
    [SerializeField] float healthRegenCooldown = 3f;
    [SerializeField] float healthRegenAccumulator = 0f;
    [SerializeField] float healthRegenRate = 2f;

    [SerializeField] float energyRegenDelay = 0.5f;
    [SerializeField] float energyRegenRateNormal = 5f;
    [SerializeField] float energyRegenRateCooldown = 10f;

    public bool energyCooldown = false;
    public bool healthCooldown = false;
    public bool energyDelay = false;

    // External usage flag (set by WeaponManager / PlayerController)
    [HideInInspector] public bool IsUsingEnergy = false;

    Coroutine energyCooldownCoroutine;
    Coroutine healthRegenCoroutine;
    Coroutine energyRegenCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }

        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<UIManager>();
        }

        if (weaponManager == null)
        {
            weaponManager = FindFirstObjectByType<WeaponManager>();
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        uiManager.UpdateHealthBar();

        // Reset health regen coroutine so regen delay restarts on each hit
        if (healthRegenCoroutine != null)
        {
            StopCoroutine(healthRegenCoroutine);
            healthRegenCoroutine = null;
        }
        healthRegenCoroutine = StartCoroutine(HealthRegenDelay());
        healthRegenAccumulator = 0f;

        if (health < 0)
        {
            Die();
        }
    }


    public void UseEnergy(float amount)
    {
        energy -= amount;
        uiManager.UpdateEnergyBar();
        if (energy < 0) energy = 0;

        if (energyRegenCoroutine != null)
        {
            StopCoroutine(energyRegenCoroutine);
            energyRegenCoroutine = null;
        }
        energyRegenCoroutine = StartCoroutine(EnergyRegenDelay());

        // If we hit zero, start the energy cooldown/recovery routine
        if (energy <= 0f && energyCooldownCoroutine == null)
        {
            energyCooldownCoroutine = StartCoroutine(EnergyCooldownRecovery());
        }
    }

    public void Die()
    {
        // Add death stuff here
        SceneManager.LoadScene(0);
    }


    // Regen for energy and health
    private void Update()
    {
        bool usingEnergy = IsUsingEnergy || (weaponManager != null && weaponManager.IsFiring);

        if (!energyCooldown && !usingEnergy && !energyDelay && energy < maxEnergy)
        {
            energy += Time.deltaTime * energyRegenRateNormal;
            uiManager.UpdateEnergyBar();
            if (energy > maxEnergy) energy = maxEnergy;
        }

        if (!healthCooldown && health < maxHealth)
        {
            // Integer health so need to convert regen to int over time
            healthRegenAccumulator += Time.deltaTime * healthRegenRate;
            int heal = Mathf.FloorToInt(healthRegenAccumulator);
            if (heal > 0)
            {
                health += heal;
                healthRegenAccumulator -= heal;
                if (health > maxHealth) health = maxHealth;
                uiManager.UpdateHealthBar();
            }
        }
    }

    // When energy hits zero we start a recovery routine
    IEnumerator EnergyCooldownRecovery()
    {
        energyCooldown = true;
        uiManager?.StartEnergyCooldownFlash();

        // Regenerate 
        float target = maxEnergy * 0.3f;
        while (energy < target)
        {
            energy += Time.deltaTime * energyRegenRateCooldown;
            uiManager.UpdateEnergyBar();
            if (energy > maxEnergy) energy = maxEnergy;
            yield return null;
        }

        // Clear cooldown
        energyCooldown = false;
        uiManager?.StopEnergyCooldownFlash();

        energyCooldownCoroutine = null;
    }

    IEnumerator HealthRegenDelay()
    {
        healthCooldown = true;
        yield return new WaitForSeconds(healthRegenCooldown);
        healthCooldown = false;
        healthRegenCoroutine = null;
    }

    IEnumerator EnergyRegenDelay()
    {
        energyDelay = true;
        yield return new WaitForSeconds(energyRegenDelay);
        energyDelay = false;
        energyRegenCoroutine = null;
    }
}
