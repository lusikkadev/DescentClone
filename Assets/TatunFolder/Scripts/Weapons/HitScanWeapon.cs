using UnityEngine;

public class HitScanWeapon : WeaponBase
{
    [Header("Hitscan")]
    public float range = 200f;
    public float damage = 10f;
    public GameObject impactPrefab;
    public GameObject tracerPrefab;
    public float tracerLifetime = 0.2f;

    public override void Fire(Ray aimRay)
    {
        if (!CanFire()) return;
        NoteFire();

        // DEBUG: quick diagnostics
        Debug.Log($"[HitScanWeapon] Fire called. muzzle={(muzzle == null ? "null" : muzzle.name)}, tracerPrefab={(tracerPrefab == null ? "null" : tracerPrefab.name)}, impactPrefab={(impactPrefab == null ? "null" : impactPrefab.name)}");

        Ray ray = aimRay;
        if (Physics.Raycast(ray, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            Debug.Log($"[HitScanWeapon] Ray hit {hit.collider.name} at {hit.point}");
            var dmg = hit.collider.GetComponent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(damage);
                Debug.Log($"[HitScanWeapon] Applied {damage} damage to {hit.collider.name}");
            }

            if (impactPrefab != null)
                Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

            if (tracerPrefab != null && muzzle != null)
            {
                var dir = (hit.point - muzzle.position);
                var t = Instantiate(tracerPrefab, muzzle.position, Quaternion.LookRotation(dir));
                Destroy(t, tracerLifetime);
            }
            else if (tracerPrefab == null)
            {
                Debug.Log("[HitScanWeapon] tracerPrefab is null - no tracer spawned");
            }
            else
            {
                Debug.Log("[HitScanWeapon] muzzle is null - cannot spawn tracer");
            }
        }
        else
        {
            Debug.Log("[HitScanWeapon] Raycast did not hit anything");
            if (tracerPrefab != null && muzzle != null)
            {
                var t = Instantiate(tracerPrefab, muzzle.position, Quaternion.LookRotation(ray.direction));
                Destroy(t, tracerLifetime);
            }
            else if (tracerPrefab == null)
            {
                Debug.Log("[HitScanWeapon] tracerPrefab is null - no tracer spawned on miss");
            }
            else
            {
                Debug.Log("[HitScanWeapon] muzzle is null - cannot spawn tracer on miss");
            }
        }
    }
}