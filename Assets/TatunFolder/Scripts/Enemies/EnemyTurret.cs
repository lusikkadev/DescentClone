using System.Collections;
using UnityEngine;

public class EnemyTurret : MonoBehaviour, IDamageable
{
    [Header("References")]
    public Transform firingPosition;
    [Tooltip("Optional: multiple firing points. If set, these will be used instead of single firingPosition.")]
    public Transform[] firingPositions;
    public GameObject projectilePrefab;

    [Header("Firing")]
    public float fireInterval = 1f;
    public float projectileSpeed = 60f;

    [Header("Multi-muzzle firing")]
    public bool sequentialMuzzleFire = false;
    public float muzzleSequentialDelay = 0.08f;

    [Tooltip("Maximum cone angle in degrees for firing inaccuracy. Set to 0 for perfectly forward shots.")]
    public float aimSpreadDegrees = 6f;
    [Tooltip("0 = fire straight from muzzle, 1 = fully lead shots using target velocity/ projectile speed.")]
    [Range(0f, 1f)]
    public float aimPredictionFactor = 0.6f;

    [Header("Rotation")]
    public float rotationSpeed = 6f;

    [Header("Death")]
    public ParticleSystem deathFX;
    public GameObject body;
    [Tooltip("Scale multiplier applied to the instantiated death FX (useful for larger bosses)")]
    public float deathFXScale = 1f;

    [Header("Debug")]
    public bool drawDebugGizmos = false;

    [Header("Health")]
    public int health = 20;

    float fireTimer = 0f;
    EnemyMover parentMover;
    Transform player;
    Rigidbody playerRb;
    Rigidbody rb;
    Quaternion desiredRotation;
    bool useRbRotation = false;

    void Awake()
    {
        parentMover = GetComponentInParent<EnemyMover>();
        player = FindObjectOfType<PlayerController>()?.transform;
        playerRb = player != null ? player.GetComponent<Rigidbody>() : null;
        rb = GetComponent<Rigidbody>();
        useRbRotation = rb != null && !rb.isKinematic;
        desiredRotation = transform.rotation;
    }

    void Update()
    {
        fireTimer += Time.deltaTime;
        if (player == null) return;

        // compute desired rotation to face player
        ComputeDesiredRotation(player.position);

        // if no rigidbody to drive rotation, apply directly
        if (!useRbRotation)
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, Mathf.Clamp01(rotationSpeed * Time.deltaTime));

        // firing
        bool hasLOS = HasLineOfSightTo(player.position);
        // if turret sees player, alert parent mover so boss keeps chasing even if boss body can't see
        if (hasLOS && parentMover != null)
            parentMover.AlertAt(player.position);

        if (fireTimer >= fireInterval && hasLOS)
        {
            FireAtPlayer();
            fireTimer = 0f;
        }
    }

    void FixedUpdate()
    {
        if (useRbRotation && rb != null)
        {
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, desiredRotation, Mathf.Clamp01(rotationSpeed * Time.fixedDeltaTime)));
        }
    }

    void ComputeDesiredRotation(Vector3 worldTarget)
    {
        Vector3 toTarget = worldTarget - transform.position;
        if (toTarget.sqrMagnitude < 0.01f) return;
        desiredRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0) Die();
    }

    void Die()
    {
        StartCoroutine(DeathRoutine());
    }

    IEnumerator DeathRoutine()
    {
        if (deathFX != null)
        {
            ParticleSystem fx = Instantiate(deathFX, transform.position, Quaternion.identity);
            if (fx != null)
            {
                fx.transform.SetParent(null);
                var main = fx.main;
                // apply optional scaling for larger bosses: scale transform and particle sizes
                if (Mathf.Abs(deathFXScale - 1f) > 1e-6f)
                {
                    fx.transform.localScale = Vector3.one * deathFXScale;
                    // multiply particle start size multiplier so particles scale regardless of simulation space
                    main.startSizeMultiplier *= deathFXScale;
                }
                fx.Play();

                float life = main.duration;
                float startLifetime = 0f;
                if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants || main.startLifetime.mode == ParticleSystemCurveMode.TwoCurves)
                    startLifetime = main.startLifetime.constantMax;
                else
                    startLifetime = main.startLifetime.constant;
                float total = life + startLifetime + 0.25f;
                Destroy(fx.gameObject, total);
            }
        }

        AudioFW.Play(id: "EnemyDeath");

        if (body != null)
        {
            body.SetActive(false);
        }
        else
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
                if (r != null) r.enabled = false;
        }

        var cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
            if (c != null) c.enabled = false;

        // stop firing and movement
        if (rb != null) rb.angularVelocity = Vector3.zero;
        this.enabled = false;

        yield return new WaitForSeconds(3.0f);
        Destroy(gameObject);
    }

    bool HasLineOfSightTo(Vector3 worldPos)
    {
        Vector3 origin = firingPosition != null ? firingPosition.position : transform.position;
        Vector3 dir = worldPos - origin;
        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude))
        {
            if (player != null && (hit.collider.transform.IsChildOf(player) || hit.collider.transform == player)) return true;
            return false;
        }
        return true;
    }

    void RotateToward(Vector3 worldTarget)
    {
        // kept for compatibility but actual rotation is handled by ComputeDesiredRotation + FixedUpdate/Update
        ComputeDesiredRotation(worldTarget);
    }

    void FireAtPlayer()
    {
        if (projectilePrefab == null || player == null) return;

        Transform[] muzzles = firingPositions != null && firingPositions.Length > 0 ? firingPositions : (firingPosition != null ? new Transform[] { firingPosition } : null);
        if (muzzles == null || muzzles.Length == 0) return;

        if (sequentialMuzzleFire)
        {
            StartCoroutine(FireMuzzlesSequential(muzzles));
        }
        else
        {
            foreach (var m in muzzles)
                SpawnProjectileFromMuzzle(m);
        }
    }

    IEnumerator FireMuzzlesSequential(Transform[] muzzles)
    {
        for (int i = 0; i < muzzles.Length; i++)
        {
            var m = muzzles[i];
            if (m != null) SpawnProjectileFromMuzzle(m);
            if (i < muzzles.Length - 1)
                yield return new WaitForSeconds(muzzleSequentialDelay);
        }
    }

    void SpawnProjectileFromMuzzle(Transform muzzle)
    {
        if (muzzle == null) return;
        Vector3 shooterPos = muzzle.position;
        Vector3 shooterVel = parentMover != null && parentMover.Rb != null ? parentMover.Rb.linearVelocity : Vector3.zero;

        Vector3 aimDir = muzzle.forward;

        Vector3 targetVel = playerRb != null ? playerRb.linearVelocity : Vector3.zero;
        Vector3 leadDir = ComputeLeadDirection(shooterPos, player.position, shooterVel, targetVel, projectileSpeed);

        if (aimPredictionFactor > 0f)
        {
            aimDir = Vector3.Slerp(aimDir, leadDir, Mathf.Clamp01(aimPredictionFactor)).normalized;
        }

        if (aimSpreadDegrees > 0f)
        {
            float yaw = Random.Range(-aimSpreadDegrees, aimSpreadDegrees);
            float pitch = Random.Range(-aimSpreadDegrees, aimSpreadDegrees);
            aimDir = Quaternion.AngleAxis(yaw, muzzle.up) * Quaternion.AngleAxis(pitch, muzzle.right) * aimDir;
            aimDir.Normalize();
        }

        var proj = Instantiate(projectilePrefab, shooterPos, Quaternion.LookRotation(aimDir));
        Rigidbody prb = proj.GetComponent<Rigidbody>();
        if (prb != null)
            prb.linearVelocity = shooterVel + aimDir * projectileSpeed;

        AudioFW.Play(id: "EnemyLaser", pos: prb != null ? prb.position : shooterPos);
    }

    Vector3 ComputeLeadDirection(Vector3 shooterPos, Vector3 targetPos, Vector3 shooterVel, Vector3 targetVel, float projSpeed)
    {
        Vector3 rel = targetPos - shooterPos;
        Vector3 relV = targetVel - shooterVel;
        float t = rel.magnitude / Mathf.Max(0.01f, projSpeed);
        for (int i = 0; i < 4; i++)
        {
            Vector3 pred = targetPos + relV * t;
            float newT = (pred - shooterPos).magnitude / Mathf.Max(0.01f, projSpeed);
            if (Mathf.Abs(newT - t) < 0.01f) break;
            t = newT;
        }
        Vector3 aimPoint = targetPos + relV * t;
        return (aimPoint - shooterPos).normalized;
    }

    void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;
        Gizmos.color = Color.green;
        if (firingPosition != null) Gizmos.DrawLine(transform.position, firingPosition.position);
    }
}
