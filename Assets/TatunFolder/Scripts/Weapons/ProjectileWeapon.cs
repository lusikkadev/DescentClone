using UnityEngine;


public class ProjectileWeapon : WeaponBase
{
    [Header("Projectile")]
    [Tooltip("Prefab to spawn when firing (should have Projectile component)")]
    public GameObject projectilePrefab;
    
    [Tooltip("Initial speed of the projectile")]
    public float projectileSpeed = 100f;
    
    [Tooltip("Forward offset from muzzle to avoid self-collision")]
    public float spawnOffset = 0.5f;

    public override void Fire(Ray aimRay)
    {
        if (!CanFire() || projectilePrefab == null || muzzle == null) return;
        NoteFire();

        Vector3 spawnPos = muzzle.position + muzzle.forward * spawnOffset;
        Quaternion spawnRot = Quaternion.LookRotation(aimRay.direction);

        var go = Instantiate(projectilePrefab, spawnPos, spawnRot);
        var proj = go.GetComponent<Projectile>();
        if (proj != null)
        {
            Vector3 inherit = ownerRb != null ? ownerRb.linearVelocity : Vector3.zero;
            proj.Initialize(aimRay.direction.normalized * projectileSpeed, inherit, ownerRb, hitMask);
        }
    }
}