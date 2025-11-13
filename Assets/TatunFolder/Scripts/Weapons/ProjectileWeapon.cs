using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Collections;


public class ProjectileWeapon : WeaponBase
{
    [Header("Projectile")]
    [Tooltip("Prefab to spawn when firing (should have Projectile component)")]
    public GameObject projectilePrefab;

    [Tooltip("Initial speed of the projectile")]
    public float projectileSpeed = 100f;

    [Tooltip("Forward offset from muzzle to avoid self-collision")]
    public float spawnOffset = 0.5f;

    [Header("Multi-muzzle")]
    [Tooltip("Offsets from center aim for each muzzle (local screen units, e.g. -0.2 = left, 0.2 = right)")]
    public List<Vector2> aimOffsets = new List<Vector2>();

    [Header("Sequential Firing")]
    [SerializeField] bool sequentialFiring = false;
    [Range(0f, 1f)]
    [SerializeField] float sequentialFireDelay = 0.1f;

    [Header("Audio")]
    [SerializeField] AudioSource gun_Audio;
    [SerializeField] AudioClip shootSound;
    [SerializeField] AudioClip impactSound;

    public override void Fire(Ray aimRay)
    {
        if (!CanFire() || projectilePrefab == null || muzzles == null || muzzles.Count == 0) return;
        NoteFire();


        if (sequentialFiring)
        {
            StartCoroutine(FireSequentially(aimRay));
        }
        else
        {
            for (int i = 0; i < muzzles.Count; i++)
            {
                FireAllMuzzles(i, aimRay);
            }
        }
    }

    private IEnumerator FireSequentially(Ray aimRay)
    {
        for (int i = 0; i < muzzles.Count; i++)
        {
            FireAllMuzzles(i, aimRay);
            if (i < muzzles.Count - 1 && sequentialFireDelay > 0f)
            {
                yield return new WaitForSeconds(sequentialFireDelay);
            }
        }
    }

    private void FireAllMuzzles(int i, Ray aimRay)
    {
        var muzzle = muzzles[i];
        Vector2 offset = (i < aimOffsets.Count) ? aimOffsets[i] : Vector2.zero;
        Ray ray = GetOffsetRay(aimRay, offset);

        Vector3 spawnPos = muzzle.position + muzzle.forward * spawnOffset;
        Quaternion spawnRot = Quaternion.LookRotation(ray.direction);

        var go = Instantiate(projectilePrefab, spawnPos, spawnRot);
        var proj = go.GetComponent<Projectile>();
        if (proj != null)
        {
            Vector3 inherit = ownerRb != null ? ownerRb.linearVelocity : Vector3.zero;
            proj.Initialize(ray.direction.normalized * projectileSpeed, inherit, ownerRb, hitMask);
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