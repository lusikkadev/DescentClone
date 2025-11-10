using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Base class for all weapons in the Descent-style weapon system.
/// Inherit from this to create new weapon types (hitscan, projectile, etc.).
/// </summary>
public abstract class WeaponBase : MonoBehaviour
{
    [Header("Common")]
    [Tooltip("Where projectiles / tracers spawn (can be camera or ship)")]
    public List<Transform> muzzles = new List<Transform>();
    
    [Tooltip("Minimum time between shots in seconds")]
    public float cooldown = 0.1f;
    
    [Tooltip("Layer mask for what this weapon can hit")]
    public LayerMask hitMask = ~0; // default all

    protected Camera aimCamera;
    protected Rigidbody ownerRb;
    float lastFireTime = -999f;

    /// <summary>
    /// Initialize weapon with references to camera and owner rigidbody.
    /// Called by WeaponManager when weapon is added.
    /// </summary>
    public virtual void Initialize(Camera cam, Rigidbody owner)
    {
        aimCamera = cam;
        ownerRb = owner;
    }

    /// <summary>
    /// Check if enough time has passed since last fire to fire again.
    /// </summary>
    protected bool CanFire()
    {
        return Time.time - lastFireTime >= cooldown;
    }

    /// <summary>
    /// Record that the weapon was fired (updates cooldown timer).
    /// </summary>
    protected void NoteFire()
    {
        lastFireTime = Time.time;
    }

    /// <summary>
    /// Fire the weapon using the provided aim ray (origin + direction).
    /// Implement this in derived classes to define weapon behavior.
    /// </summary>
    public abstract void Fire(Ray aimRay);

    /// <summary>
    /// Backwards-compatible convenience: compute center-screen ray from stored aimCamera and call Fire(Ray).
    /// </summary>
    public virtual void Fire()
    {
        if (aimCamera == null) return;
        Vector2 center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Ray ray = aimCamera.ScreenPointToRay(center);
        Fire(ray);
    }

    /// <summary>
    /// Called when this weapon is equipped.
    /// Override to add custom behavior (e.g., play sound, enable visuals).
    /// </summary>
    public virtual void OnEquip() { }
    
    /// <summary>
    /// Called when this weapon is unequipped.
    /// Override to add custom behavior (e.g., stop sounds, disable visuals).
    /// </summary>
    public virtual void OnUnequip() { }
}