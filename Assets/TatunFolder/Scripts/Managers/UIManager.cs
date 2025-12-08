using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] Slider healthBar;
    [SerializeField] Slider energyBar;

    [SerializeField] Image hudImage;
    Color hudColor;

    StatManager statManager;

    Image energyFillImage;
    Coroutine energyFlashCoroutine;

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

        if (energyBar != null && energyBar.fillRect != null)
        {
            energyFillImage = energyBar.fillRect.GetComponent<Image>();
        }

        energyFillImage.color = Color.lightBlue;
    }

    void Start()
    {
        UpdateHealthBar();
        UpdateEnergyBar();

        hudColor = hudImage.color;
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

    // Flashing helpers for energy cooldown state
    public void StartEnergyCooldownFlash()
    {
        if (energyFillImage == null) return;
        if (hudImage == null) return;
        if (energyFlashCoroutine != null) StopCoroutine(energyFlashCoroutine);
        energyFlashCoroutine = StartCoroutine(EnergyFlashRoutine());
    }

    public void StopEnergyCooldownFlash()
    {
        if (energyFlashCoroutine != null)
        {
            StopCoroutine(energyFlashCoroutine);
            energyFlashCoroutine = null;
        }
        // restore fill alpha/color
        if (energyFillImage != null)
        {
            energyFillImage.color = Color.lightBlue;
        }
        if (hudImage != null)
        {
            hudImage.color = hudColor;
        }
    }

    IEnumerator EnergyFlashRoutine()
    {
        if (energyFillImage == null) yield break;
        if (hudImage == null) yield break;
        Color baseColor = Color.lightBlue;
        Color flashColor = Color.white;
        Color hudFlashColor = Color.grey;
        float interval = 0.35f;

        while (true)
        {
            energyFillImage.color = flashColor;
            hudImage.color = hudFlashColor;
            yield return new WaitForSeconds(interval);
            energyFillImage.color = baseColor;
            hudImage.color = hudColor;
            yield return new WaitForSeconds(interval);
        }
    }
}
