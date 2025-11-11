using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour
{
    public float lifeTime = 10f;
    public float damage = 20f;
    Rigidbody rb;
    Rigidbody ownerRb;
    LayerMask hitMask = ~0;
    [SerializeField] ParticleSystem explosionEffect;
    [SerializeField] ParticleSystem trailEffect;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, lifeTime);
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
        MeshRenderer mr = GetComponentInChildren<MeshRenderer>();
        mr.enabled = false;
        // damage
        var dmg = collision.collider.GetComponent<IDamageable>();
        if (dmg != null) dmg.TakeDamage(damage);

        explosionEffect?.Play();
        StartCoroutine(DestroyAfterEffect());
    }

    IEnumerator DestroyAfterEffect()
    {
        // stop trail
        if (trailEffect != null)
        {
            trailEffect.transform.parent = null;
            var trailMain = trailEffect.main;
            trailMain.stopAction = ParticleSystemStopAction.Destroy;
            trailEffect.Stop();
        }
        // wait for explosion effect to finish
        if (explosionEffect != null)
        {
            var explosionMain = explosionEffect.main;
            yield return new WaitForSeconds(explosionMain.duration + explosionMain.startLifetime.constantMax);
        }
        Destroy(gameObject);
    }
}