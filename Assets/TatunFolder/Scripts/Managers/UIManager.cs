using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Slider healthBar;
    public Slider energyBar;

    StatManager statManager;

    private void Awake()
    {
        if (statManager == null)
        {
            statManager = FindFirstObjectByType<StatManager>();
        }
        if (healthBar == null)
        {
            healthBar = GameObject.Find("HealthBar").GetComponent<Slider>();
        }
        if (energyBar == null)
        {
            energyBar = GameObject.Find("EnergyBar").GetComponent<Slider>();
        }
    }

    void Start()
    {
        UpdateHealthBar();
        UpdateEnergyBar();
    }

    public void UpdateHealthBar()
    {
        var previousHealth = healthBar.value;
        var currentHealth = statManager.health;

        // smooth transition from previousHealth to currentHealth
        StartCoroutine(SmoothHealthTransition(previousHealth, currentHealth, 0.2f));
    }

    IEnumerator SmoothHealthTransition(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            healthBar.value = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        healthBar.value = to;
    }

    public void UpdateEnergyBar()
    {
        var previousEnergy = energyBar.value;
        var currentEnergy = statManager.energy;

        // smooth transition from previousEnergy to currentEnergy
        StartCoroutine(SmoothEnergyTransition(previousEnergy, currentEnergy, 0.2f));
    }

    IEnumerator SmoothEnergyTransition(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            energyBar.value = Mathf.Lerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        energyBar.value = to;
    }
}
