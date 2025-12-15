using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour
{
    public float lifeTime = 10f;
    public int damage = 20;
    Rigidbody rb;
    Rigidbody ownerRb;
    public LayerMask hitMask = ~0;
    [SerializeField] ParticleSystem explosionEffect;
    [SerializeField] ParticleSystem trailEffect;
    [SerializeField] GameObject shockWave;
    [SerializeField] HitScript hitShader;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, lifeTime);
        hitShader = FindFirstObjectByType<HitScript>();
    }

    public void Initialize(Vector3 initialVelocity, Vector3 inheritVelocity, Rigidbody owner, LayerMask mask)
    {
        trailEffect?.Play();
        ownerRb = owner;
        hitMask = mask;
        rb.linearVelocity = inheritVelocity + initialVelocity;
        // avoid hitting owner directly
        if (owner != null)
        {
            var ownerCols = owner.GetComponentsInChildren<Collider>();
            var projCols = GetComponentsInChildren<Collider>();
            foreach (var pc in projCols)
                foreach (var oc in ownerCols)
                    if (pc != null && oc != null)
                        Physics.IgnoreCollision(pc, oc);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (((1 << collision.gameObject.layer) & hitMask) == 0) return;
        rb.linearVelocity = Vector3.zero;
        hitShader.Shoot(collision.contacts[0].point);
        AudioFW.Play(id: "MissileExplosion");
        // enable shockwave
        if (shockWave != null)
        {
            shockWave.transform.parent = null;
            shockWave.SetActive(true);
        }
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        mr.enabled = false;
        // damage
        var dmg = collision.collider.GetComponent<IDamageable>();
        if (dmg != null) {
            dmg.TakeDamage(damage);
        }
        else
        {
            var parentDmg = collision.collider.GetComponentInParent<IDamageable>();
            if (parentDmg != null) parentDmg.TakeDamage(damage);
            else
            {
                collision.collider.SendMessageUpwards("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }
        }

        // Prepare explosion and trail particle systems in a way that avoids reusing shared prefab instances
        ParticleSystem spawnedExplosion = null;
        ParticleSystem spawnedTrail = null;

        if (explosionEffect != null)
        {
            if (explosionEffect.transform.IsChildOf(transform))
            {
                // this projectile has its own explosion instance: detach and play
                spawnedExplosion = explosionEffect;
                spawnedExplosion.transform.parent = null;
                spawnedExplosion.Play();
            }
            else
            {
                // explosionEffect refers to a shared asset: instantiate a copy at contact point
                var go = Instantiate(explosionEffect.gameObject, collision.contacts[0].point, Quaternion.identity);
                spawnedExplosion = go.GetComponent<ParticleSystem>();
                if (spawnedExplosion != null) spawnedExplosion.Play();
            }
        }

        if (trailEffect != null)
        {
            if (trailEffect.transform.IsChildOf(transform))
            {
                // detach running trail so it continues independently
                spawnedTrail = trailEffect;
                spawnedTrail.transform.parent = null;
                var trailMain = spawnedTrail.main;
                trailMain.stopAction = ParticleSystemStopAction.Destroy;
                spawnedTrail.Stop();
            }
            else
            {
                // shared trail asset - instantiate a copy at current position
                var tgo = Instantiate(trailEffect.gameObject, trailEffect.transform.position, trailEffect.transform.rotation);
                spawnedTrail = tgo.GetComponent<ParticleSystem>();
                if (spawnedTrail != null)
                {
                    var trailMain2 = spawnedTrail.main;
                    trailMain2.stopAction = ParticleSystemStopAction.Destroy;
                    spawnedTrail.Stop();
                }
            }
        }

        StartCoroutine(DestroyAfterEffect(spawnedExplosion, spawnedTrail));
    }

    IEnumerator DestroyAfterEffect(ParticleSystem spawnedExplosion, ParticleSystem spawnedTrail)
    {
        // If trail was detached/instantiated above, ensure it will be destroyed when finished
        if (spawnedTrail != null)
        {
            // already set stopAction and stopped above
        }

        // wait for explosion effect to finish
        if (spawnedExplosion != null)
        {
            var explosionMain = spawnedExplosion.main;
            float startLifetime = 0f;
            if (explosionMain.startLifetime.mode == ParticleSystemCurveMode.TwoConstants || explosionMain.startLifetime.mode == ParticleSystemCurveMode.TwoCurves)
                startLifetime = explosionMain.startLifetime.constantMax;
            else
                startLifetime = explosionMain.startLifetime.constant;

            yield return new WaitForSeconds(explosionMain.duration + startLifetime + 0.05f);
            // destroy spawned explosion object
            Destroy(spawnedExplosion.gameObject);
        }
        else
        {
            // no explosion: small delay to allow other effects
            yield return new WaitForSeconds(0.05f);
        }

        Destroy(gameObject);
    }
}