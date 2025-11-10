using UnityEngine;



public class HitScanWeapon : WeaponBase
{
    [Header("Hitscan")]
    [Tooltip("Maximum range of the weapon")]
    public float range = 200f;

    [Tooltip("Damage dealt per hit")]
    public float damage = 10f;

    [Tooltip("Visual effect spawned at hit point (optional)")]
    public GameObject impactPrefab;

    [Tooltip("Tracer/beam visual effect (optional)")]
    public GameObject tracerPrefab;

    [Tooltip("How long tracer visual lasts")]
    public float tracerLifetime = 0.2f;

    //public override void Fire(Ray aimRay)
    //{
    //    if (!CanFire()) return;
    //    NoteFire();

    //    Debug.Log($"[HitScanWeapon] Fire called. muzzle={(muzzle == null ? "null" : muzzle.name)}, tracerPrefab={(tracerPrefab == null ? "null" : tracerPrefab.name)}, impactPrefab={(impactPrefab == null ? "null" : impactPrefab.name)}");

    //    Ray ray = aimRay;
    //    if (Physics.Raycast(ray, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
    //    {
    //        Debug.Log($"[HitScanWeapon] Ray hit {hit.collider.name} at {hit.point}");
    //        var dmg = hit.collider.GetComponent<IDamageable>();
    //        if (dmg != null)
    //        {
    //            dmg.TakeDamage(damage);
    //            Debug.Log($"[HitScanWeapon] Applied {damage} damage to {hit.collider.name}");
    //        }

    //        if (impactPrefab != null)
    //            Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));

    //        if (tracerPrefab != null && muzzle != null)
    //        {
    //            var dir = (hit.point - muzzle.position);
    //            var t = Instantiate(tracerPrefab, muzzle.position, Quaternion.LookRotation(dir));

    //            // If tracer prefab contains BeamTracer, call to stretch it
    //            var beam = t.GetComponent<BeamTracer>();
    //            if (beam != null)
    //            {
    //                beam.Setup(muzzle.position, hit.point, tracerLifetime);
    //            }
    //            else
    //            {
    //                // fallback: no tracer, just destroy after lifetime
    //                Destroy(t, tracerLifetime);
    //            }
    //        }
    //        else if (tracerPrefab == null)
    //        {
    //            Debug.Log("[HitScanWeapon] tracerPrefab is null - no tracer spawned");
    //        }
    //        else
    //        {
    //            Debug.Log("[HitScanWeapon] muzzle is null - cannot spawn tracer");
    //        }
    //    }
    //    else
    //    {
    //        Debug.Log("[HitScanWeapon] Raycast did not hit anything");
    //        if (tracerPrefab != null && muzzle != null)
    //        {
    //            var t = Instantiate(tracerPrefab, muzzle.position, Quaternion.LookRotation(ray.direction));
    //            var beam = t.GetComponent<BeamTracer>();
    //            if (beam != null)
    //            {
    //                // extend tracer a fixed distance forward when missing, if using BeamTracer
    //                beam.Setup(muzzle.position, muzzle.position + ray.direction * range, tracerLifetime);
    //            }
    //            else
    //            {
    //                Destroy(t, tracerLifetime);
    //            }
    //        }
    //        else if (tracerPrefab == null)
    //        {
    //            Debug.Log("[HitScanWeapon] tracerPrefab is null - no tracer spawned on miss");
    //        }
    //        else
    //        {
    //            Debug.Log("[HitScanWeapon] muzzle is null - cannot spawn tracer on miss");
    //        }
    //    }
    //}

    public override void Fire(Ray aimRay)
    {
        if (!CanFire()) return;
        NoteFire();

        Ray ray = aimRay;
        Vector3 start = muzzle != null ? muzzle.position : ray.origin;
        Vector3 end;

        if (Physics.Raycast(ray, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            end = hit.point;

            var dmg = hit.collider.GetComponent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(damage);

            if (impactPrefab != null)
                Instantiate(impactPrefab, hit.point, Quaternion.LookRotation(hit.normal));
        }
        else
        {
            end = start + ray.direction * range;
        }

        if (tracerPrefab != null && muzzle != null)
        {
            var t = Instantiate(tracerPrefab, start, Quaternion.LookRotation(end - start));
            var beam = t.GetComponent<BeamTracer>();
            if (beam != null)
            {
                // Use BeamTracer's inspector settings for the spline
                beam.SetupSpline(start, end, tracerLifetime);
            }
            else
            {
                Destroy(t, tracerLifetime);
            }
        }
    }


}