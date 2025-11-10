using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Projectile : MonoBehaviour
{
    public float lifeTime = 10f;
    public float damage = 20f;
    Rigidbody rb;
    Rigidbody ownerRb;
    LayerMask hitMask = ~0;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector3 initialVelocity, Vector3 inheritVelocity, Rigidbody owner, LayerMask mask)
    {
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

        // damage
        var dmg = collision.collider.GetComponent<IDamageable>();
        if (dmg != null) dmg.TakeDamage(damage);

        // add sfx etc
        Destroy(gameObject);
    }
}