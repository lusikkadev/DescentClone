using System.Collections.Generic;
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

    [Header("Multi-muzzle")]
    [Tooltip("Offsets from center aim for each muzzle (local screen units, e.g. -0.2 = left, 0.2 = right)")]
    public List<Vector2> aimOffsets = new List<Vector2>(); // e.g. (-0.2,0), (0.2,0) for dual lasers

    public override void Fire(Ray aimRay)
    {
        if (!CanFire()) return;
        NoteFire();

        // If no muzzles, fallback to old logic
        if (muzzles == null || muzzles.Count == 0)
        {
            
            return;
        }

        for (int i = 0; i < muzzles.Count; i++)
        {
            var muzzle = muzzles[i];
            Vector2 offset = (i < aimOffsets.Count) ? aimOffsets[i] : Vector2.zero;

            // Offset the aim ray for this muzzle
            Ray ray = GetOffsetRay(aimRay, offset);

            Vector3 start = muzzle != null ? muzzle.position : ray.origin;
            Vector3 end;

            if (Physics.Raycast(ray, out var hit, range, hitMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point;
                var dmg = hit.collider.GetComponent<IDamageable>();
                if (dmg != null) dmg.TakeDamage(damage);
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
                    beam.SetupSpline(start, end, tracerLifetime);
                else
                    Destroy(t, tracerLifetime);
            }
        }
    }

    // Helper: Offset the aim ray in screen space
    Ray GetOffsetRay(Ray baseRay, Vector2 screenOffset)
    {
        if (aimCamera == null || screenOffset == Vector2.zero)
            return baseRay;

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 screenPos = screenCenter + new Vector2(screenOffset.x * Screen.width, screenOffset.y * Screen.height);
        return aimCamera.ScreenPointToRay(screenPos);
    }
}