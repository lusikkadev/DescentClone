using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

/// <summary>
/// Manages multiple weapons for a player/ship, handling weapon switching, input, and firing.
/// Supports both prefab references and scene-instantiated weapons for modularity.
/// </summary>
public class WeaponManager : MonoBehaviour
{
    [Header("References")]
    public Camera aimCamera; // assign the main camera (child camera)
    public Rigidbody ownerRb; // assign player's rigidbody
    
    [Tooltip("List of weapons. Can be prefab references or scene objects. Prefabs will be auto-instantiated.")]
    public List<WeaponBase> weapons = new List<WeaponBase>();
    int currentIndex = 0;

    [Header("Input (optional)")]
    [Tooltip("If true WeaponManager will enable/handle the provided fireAction. Otherwise call FireCurrent() from your input code.")]
    public bool handleInput = true;
    public InputActionReference fireAction;
    public bool fireHoldToAuto = true;
    public float fireHoldThreshold = 0.5f;

    [Header("Aim smoothing (stabilize CameraSway)")]
    [Tooltip("0 = no smoothing. Higher values smooth more quickly (recommended 4..12).")]
    public float aimSmoothing = 8f;

    // internal smoothing state
    Vector3 smoothedAimDir = Vector3.forward;
    Ray lastSmoothedAimRay = new Ray(Vector3.zero, Vector3.forward);

    void Start()
    {
        if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
        if (ownerRb == null) ownerRb = GetComponent<Rigidbody>();

        // Instantiate any prefab weapons (not already in scene hierarchy)
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i] != null && !weapons[i].gameObject.scene.IsValid())
            {
                // This weapon is a prefab reference, not an instance in the scene
                var weaponInstance = Instantiate(weapons[i], transform);
                weaponInstance.gameObject.SetActive(false);
                weapons[i] = weaponInstance;
            }
        }

        foreach (var w in weapons)
        {
            if (w != null) w.Initialize(aimCamera, ownerRb);
            w?.gameObject.SetActive(false);
        }
        if (weapons.Count > 0 && weapons[0] != null)
        {
            weapons[0].gameObject.SetActive(true);
            weapons[0].OnEquip();
        }

        // init smoothing direction from camera if available
        if (aimCamera != null)
        {
            smoothedAimDir = aimCamera.transform.forward.normalized;
            lastSmoothedAimRay = new Ray(aimCamera.transform.position, smoothedAimDir);
        }
    }

    void OnEnable()
    {
        if (handleInput && fireAction?.action != null)
        {
            fireAction.action.Enable();
            if (!fireHoldToAuto)
            {
                // avoid duplicate subscriptions if OnEnable is called multiple times
                fireAction.action.performed -= OnFirePerformed;
                fireAction.action.performed += OnFirePerformed;
            }
        }
    }

    void OnDisable()
    {
        if (handleInput && fireAction?.action != null)
        {
            if (!fireHoldToAuto)
                fireAction.action.performed -= OnFirePerformed;
            fireAction.action.Disable();
        }
    }

    void Update()
    {
        // update aim ray (smoothed) every frame so input callbacks can use latest
        if (aimCamera == null) aimCamera = GetComponentInChildren<Camera>();
        if (aimCamera == null) return;

        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Ray raw = aimCamera.ScreenPointToRay(center);
        Vector3 rawDir = raw.direction.normalized;

        if (smoothedAimDir == Vector3.zero) smoothedAimDir = rawDir;

        if (aimSmoothing <= 0f)
        {
            smoothedAimDir = rawDir;
        }
        else
        {
            float t = 1f - Mathf.Exp(-aimSmoothing * Time.deltaTime); // exponential smoothing factor
            smoothedAimDir = Vector3.Slerp(smoothedAimDir, rawDir, t).normalized;
        }

        lastSmoothedAimRay = new Ray(raw.origin, smoothedAimDir);

        // input handling (poll when holding)
        if (handleInput && fireHoldToAuto && fireAction?.action != null)
        {
            
            float v = ReadFloat(fireAction);
            if (v >= fireHoldThreshold)
            {
                Debug.Log("player fired the gun");
                FireCurrent(lastSmoothedAimRay);
            }
        }
    }

    // called by input performed (single-press)
    void OnFirePerformed(InputAction.CallbackContext ctx)
    {
        Debug.Log("player fired the weapon");
        FireCurrent(lastSmoothedAimRay);
    }

    /// <summary>
    /// Switch to a specific weapon by index.
    /// </summary>
    /// <param name="index">Zero-based weapon index</param>
    public void SwitchTo(int index)
    {
        if (index < 0 || index >= weapons.Count) return;
        if (index == currentIndex) return; // already equipped
        
        weapons[currentIndex]?.OnUnequip();
        weapons[currentIndex]?.gameObject.SetActive(false);
        currentIndex = index;
        weapons[currentIndex]?.gameObject.SetActive(true);
        weapons[currentIndex]?.OnEquip();
    }

    /// <summary>
    /// Switch to the next weapon in the list (cycles to first if at end).
    /// </summary>
    public void NextWeapon()
    {
        if (weapons.Count <= 1) return;
        int next = (currentIndex + 1) % weapons.Count;
        SwitchTo(next);
    }

    /// <summary>
    /// Switch to the previous weapon in the list (cycles to last if at start).
    /// </summary>
    public void PreviousWeapon()
    {
        if (weapons.Count <= 1) return;
        int prev = (currentIndex - 1 + weapons.Count) % weapons.Count;
        SwitchTo(prev);
    }

    /// <summary>
    /// Add a new weapon at runtime. If it's a prefab, it will be instantiated.
    /// </summary>
    /// <param name="weapon">Weapon to add (can be prefab or instance)</param>
    /// <returns>The instantiated/added weapon instance</returns>
    public WeaponBase AddWeapon(WeaponBase weapon)
    {
        if (weapon == null) return null;
        
        WeaponBase instance = weapon;
        // If it's a prefab, instantiate it
        if (!weapon.gameObject.scene.IsValid())
        {
            instance = Instantiate(weapon, transform);
            instance.gameObject.SetActive(false);
        }
        
        instance.Initialize(aimCamera, ownerRb);
        weapons.Add(instance);
        
        // If this is the first weapon, equip it
        if (weapons.Count == 1)
        {
            currentIndex = 0;
            instance.gameObject.SetActive(true);
            instance.OnEquip();
        }
        
        return instance;
    }

    /// <summary>
    /// Remove a weapon by index.
    /// </summary>
    /// <param name="index">Index of weapon to remove</param>
    public void RemoveWeapon(int index)
    {
        if (index < 0 || index >= weapons.Count) return;
        
        var weapon = weapons[index];
        weapons.RemoveAt(index);
        
        if (weapon != null && weapon.gameObject != null)
        {
            Destroy(weapon.gameObject);
        }
        
        // Adjust current index if needed
        if (currentIndex >= weapons.Count)
        {
            currentIndex = Mathf.Max(0, weapons.Count - 1);
        }
        
        // Activate the new current weapon if there are any left
        if (weapons.Count > 0 && weapons[currentIndex] != null)
        {
            weapons[currentIndex].gameObject.SetActive(true);
            weapons[currentIndex].OnEquip();
        }
    }

    /// <summary>
    /// Get the currently equipped weapon.
    /// </summary>
    public WeaponBase GetCurrentWeapon()
    {
        if (currentIndex >= 0 && currentIndex < weapons.Count)
            return weapons[currentIndex];
        return null;
    }

    /// <summary>
    /// Get the total number of weapons.
    /// </summary>
    public int GetWeaponCount() => weapons.Count;

    /// <summary>
    /// Get the current weapon index.
    /// </summary>
    public int GetCurrentWeaponIndex() => currentIndex;

    /// <summary>
    /// Fire the currently equipped weapon (legacy API using last smoothed aim ray).
    /// </summary>
    public void FireCurrent()
    {
        FireCurrent(lastSmoothedAimRay);
    }

    /// <summary>
    /// Fire the currently equipped weapon using the provided aim ray.
    /// </summary>
    /// <param name="aimRay">The ray to use for aiming (origin + direction)</param>
    public void FireCurrent(Ray aimRay)
    {
        if (weapons.Count == 0) return;
        weapons[currentIndex]?.Fire(aimRay);
    }

    static float ReadFloat(InputActionReference prop)
    {
        if (prop == null || prop.action == null) return 0f;
        try { return prop.action.ReadValue<float>(); } catch { return 0f; }
    }
}