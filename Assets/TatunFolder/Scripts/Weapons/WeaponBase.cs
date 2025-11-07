using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [Header("Common")]
    public Transform muzzle; // where projectiles / tracers spawn (can be camera or ship)
    public float cooldown = 0.1f;
    public LayerMask hitMask = ~0; // default all

    protected Camera aimCamera;
    protected Rigidbody ownerRb;
    float lastFireTime = -999f;

    public virtual void Initialize(Camera cam, Rigidbody owner)
    {
        aimCamera = cam;
        ownerRb = owner;
    }

    protected bool CanFire()
    {
        return Time.time - lastFireTime >= cooldown;
    }

    protected void NoteFire()
    {
        lastFireTime = Time.time;
    }

    /// <summary>
    /// Fire using the provided aim ray (origin + direction). Weapon implementations should use this.
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

    public virtual void OnEquip() { }
    public virtual void OnUnequip() { }
}