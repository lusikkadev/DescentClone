using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class NewBasicEnemyAI : MonoBehaviour, IDamageable
{
    public enum State { Patrol, Chasing, Searching, Returning }

    [Header("Profile (optional)")]
    public EnemyProfileSO profile;

    [Header("References")]
    public Transform firingPosition;
    [Tooltip("Optional: multiple firing points. If set, these will be used instead of single firingPosition.")]
    public Transform[] firingPositions;
    public GameObject projectilePrefab;

    [Header("Patrol (box)")]
    public Transform patrolCenter; // default self position
    public Vector3 patrolBoxSize = new Vector3(10f, 4f, 6f);
    public int patrolPointCount = 4;
    public float patrolSpeed = 6f;
    public float patrolReach = 1.0f;
    public bool patrolRandomize = true;

    [Header("Chase / Combat")]
    public float detectionRange = 40f;
    [Range(0f, 180f)] public float detectionAngle = 90f;
    public LayerMask obstacleMask = ~0;
    public float chaseSpeed = 16f;
    public float desiredCombatDistance = 10f;
    public float fireInterval = 1f;
    public float projectileSpeed = 60f;
    public float loseSightTime = 3.0f;

    [Header("Multi-muzzle firing")]
    [Tooltip("When true, fire from muzzles sequentially with delay between each muzzle.")]
    public bool sequentialMuzzleFire = false;
    [Tooltip("Delay in seconds between firing successive muzzles when sequential firing is enabled.")]
    public float muzzleSequentialDelay = 0.08f;

    [Tooltip("Maximum cone angle in degrees for firing inaccuracy. Set to 0 for perfectly forward shots.")]
    public float aimSpreadDegrees = 6f;
    [Tooltip("0 = fire straight from muzzle, 1 = fully lead shots using target velocity/ projectile speed.")]
    [Range(0f, 1f)]
    public float aimPredictionFactor = 0.6f;

    [Header("Movement / avoidance")]
    public float maxAccel = 40f;
    public float linearDamping = 0.92f;
    public float avoidDistance = 3f;
    public int avoidRays = 7;
    public float avoidStrength = 8f;

    [Header("Evasion")]
    public float evadeAmplitude = 0.6f;
    public float evadeFrequency = 1.2f;

    [Header("Orbiting (skirmisher)")]
    [Tooltip("Enable orbiting behaviour (circle around player)")]
    public bool enableOrbit = false;
    [Tooltip("Speed (radians/sec) used when orbiting")]
    public float orbitSpeed = 2.0f;
    [Tooltip("Radius multiplier for orbit (multiplies desiredCombatDistance)")]
    public float orbitRadius = 0.6f;
    [Tooltip("Flip orbit direction")]
    public bool orbitClockwise = false;

    [Header("Rotation")]
    public float rotationSpeed = 6f;

    [Header("Alarm")]
    public float alarmRange = 18f;
    public bool alarmOthers = true;

    [Header("Stuck detection / Unstuck")]
    [Tooltip("If enemy is stuck near geometry for this many seconds, perform an unstuck impulse.")]
    public float stuckTimeThreshold = 0.6f;
    [Tooltip("Radius used to detect immediate obstacles for unstuck logic")]
    public float stuckProbeRadius = 0.6f;
    [Tooltip("Velocity threshold below which we consider the enemy 'stuck'")]
    public float stuckVelocityThreshold = 0.6f;
    [Tooltip("Impulse strength applied to push enemy out of geometry")]
    public float unstuckForce = 6f;

    [Header("Debug")]
    public bool drawDebugGizmos = false;

    // internals
    Rigidbody rb;
    Transform player;
    Rigidbody playerRb;
    [SerializeField] BossWeakSpot[] weakSpots;
    Vector3[] patrolPoints;
    int patrolIndex = 0;
    State state = State.Patrol;

    Vector3 lastKnownPlayerPos;
    float lastSeenTime = -999f;
    float fireTimer = 0f;
    float hoverSeed;

    float stuckTimer = 0f;

    [SerializeField] int health = 50;

    // Expose select internals for helper components
    public Transform Player => player;
    public Rigidbody Rb => rb;
    [Header("Boss options")]
    [Tooltip("When true, only hits to the Boss eye (via BossEye component) will reduce health. Body hits still alert the boss.")]
    public bool requireEyeHits = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        player = FindFirstObjectByType<PlayerController>()?.transform;
        playerRb = player != null ? player.GetComponent<Rigidbody>() : null;

        if (rb != null)
        { // gets stuck in walls otherwise
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Start()
    {
        hoverSeed = Random.value * 10f;

        if (patrolCenter == null)
        {
            GameObject go = new GameObject($"{name}_PatrolCenter");
            go.transform.position = transform.position;
            patrolCenter = go.transform;
        }

        BuildPatrolPoints();
        if (profile != null) ApplyProfile(profile);

        // register weak spots if present on children
        weakSpots = GetComponentsInChildren<BossWeakSpot>(true);
        if (weakSpots != null && weakSpots.Length > 0)
        {
            foreach (var ws in weakSpots)
                ws?.RegisterOwner(this);
        }
    }

    public void ApplyProfile(EnemyProfileSO p)
    {
        if (p == null) return;
        patrolSpeed = p.patrolSpeed;
        chaseSpeed = p.chaseSpeed;
        maxAccel = p.maxAccel;
        linearDamping = p.linearDamping;
        avoidDistance = p.avoidDistance;
        avoidRays = p.avoidRays;
        avoidStrength = p.avoidStrength;
        desiredCombatDistance = p.desiredCombatDistance;
        fireInterval = p.fireInterval;
        projectileSpeed = p.projectileSpeed;
        detectionRange = p.detectionRange;
        detectionAngle = p.detectionAngle;
        loseSightTime = p.loseSightTime;
        aimSpreadDegrees = p.aimSpreadDegrees;
        aimPredictionFactor = p.aimPredictionFactor;
        evadeAmplitude = p.evadeAmplitude;
        evadeFrequency = p.evadeFrequency;
        enableOrbit = p.enableOrbit;
        orbitSpeed = p.orbitSpeed;
        orbitRadius = p.orbitRadius;
        orbitClockwise = p.orbitClockwise;
        rotationSpeed = p.rotationSpeed;
        alarmRange = p.alarmRange;
        alarmOthers = p.alarmOthers;
        stuckTimeThreshold = p.stuckTimeThreshold;
        stuckProbeRadius = p.stuckProbeRadius;
        stuckVelocityThreshold = p.stuckVelocityThreshold;
        unstuckForce = p.unstuckForce;
        drawDebugGizmos = p.drawDebugGizmos;
        health = p.health;
    }

    void BuildPatrolPoints()
    {
        patrolPoints = new Vector3[patrolPointCount];
        Vector3 origin = patrolCenter.position;
        for (int i = 0; i < patrolPointCount; i++)
        {
            if (patrolRandomize)
            {
                Vector3 half = patrolBoxSize * 0.5f;
                float rx = Random.Range(-half.x, half.x);
                float ry = Random.Range(-half.y, half.y);
                float rz = Random.Range(-half.z, half.z);
                patrolPoints[i] = origin + new Vector3(rx, ry, rz);
            }
            else
            {
                float t = (float)i / patrolPointCount;
                Vector3 offs = new Vector3(
                    Mathf.Lerp(-patrolBoxSize.x / 2, patrolBoxSize.x / 2, t),
                    Mathf.Lerp(-patrolBoxSize.y / 2, patrolBoxSize.y / 2, (i % 2 == 0) ? 0.25f : 0.75f),
                    Mathf.Lerp(-patrolBoxSize.z / 2, patrolBoxSize.z / 2, 1f - t)
                );
                patrolPoints[i] = origin + offs;
            }
        }
        patrolIndex = 0;
    }

    void Update()
    {
        fireTimer += Time.deltaTime;

        if (player == null) return;

        bool canSee = CanSeePlayer();
        if (canSee)
        {
            lastKnownPlayerPos = player.position;
            lastSeenTime = Time.time;
            if (state != State.Chasing) BecomeAware();
            state = State.Chasing;

            if (alarmOthers) AlarmNearby();
        }
        else
        {
            if (state == State.Chasing && Time.time - lastSeenTime > loseSightTime)
            {
                state = State.Searching;
                StartCoroutine(SearchThenReturn());
            }
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // damping to avoid runaway drift
        rb.linearVelocity *= linearDamping;

        // stuck detection
        CheckStuckAndUnstuck();

        switch (state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chasing: UpdateChase(); break;
            case State.Searching: UpdateSearchHover(); break;
            case State.Returning: UpdateReturn(); break;
        }
    }

    void BecomeAware()
    {
        lastSeenTime = Time.time;
        fireTimer = 0f;
    }

    // --- Patrol ---
    void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Vector3 goal = patrolPoints[patrolIndex];
        MoveTowardsGoal(goal, patrolSpeed);

        if (Vector3.Distance(transform.position, goal) < patrolReach)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

        AlignToVelocity();
    }

    // --- Chase ---
    void UpdateChase()
    {
        if (player == null) return;

        // compute combat position
        Vector3 toPlayer = lastKnownPlayerPos - transform.position;
        Vector3 dir = toPlayer.normalized;

        // base evade wiggle
        Vector3 evade = (transform.right * Mathf.Sin((Time.time + hoverSeed) * evadeFrequency) +
                         transform.up * Mathf.Cos((Time.time + hoverSeed) * evadeFrequency * 0.7f)) * evadeAmplitude;

        // orbiting optional, orbits around player if checked
        Vector3 orbitOffset = Vector3.zero;
        if (enableOrbit)
        {
            Vector3 worldUp = Vector3.up;
            Vector3 tangent = Vector3.Cross(dir, worldUp);
            if (tangent.sqrMagnitude < 1e-4f) tangent = Vector3.Cross(dir, transform.up);
            tangent.Normalize();
            float sign = orbitClockwise ? -1f : 1f;
            float theta = Time.time * orbitSpeed + hoverSeed;
            orbitOffset = tangent * (Mathf.Sin(theta) * orbitRadius * desiredCombatDistance * sign);
        }

        Vector3 combatPos = lastKnownPlayerPos - dir * desiredCombatDistance + evade + orbitOffset;

        MoveTowardsGoal(combatPos, chaseSpeed);

        RotateToward(lastKnownPlayerPos);

        if (fireTimer >= fireInterval && HasLineOfSightTo(player.position))
        {
            FireAtPlayer();
            fireTimer = 0f;
        }

        // debug: draw orbit direction/offset
        if (drawDebugGizmos)
        {
            Debug.DrawLine(transform.position, combatPos, Color.magenta);
            Debug.DrawRay(lastKnownPlayerPos, orbitOffset, Color.cyan);
        }
    }

    // Search hover
    void UpdateSearchHover()
    {
        // small hover and rotate to look around
        Vector3 hover = ComputeEvade() * 0.3f;
        rb.AddForce(hover * 0.2f, ForceMode.Acceleration);
        AlignToVelocity();
    }

    void UpdateReturn()
    {
        Vector3 home = patrolPoints[patrolIndex];
        MoveTowardsGoal(home, patrolSpeed);
        AlignToVelocity();

        if (Vector3.Distance(transform.position, home) < patrolReach * 1.5f)
            state = State.Patrol;
    }

    // Mover plus avoidance
    void MoveTowardsGoal(Vector3 worldGoal, float speed)
    {
        Vector3 toGoal = worldGoal - transform.position;
        if (toGoal.sqrMagnitude < 0.01f) return;

        Vector3 desired = toGoal.normalized * speed;

        // obstacle avoidance
        Vector3 avoid = ComputeAvoidance();
        Vector3 final = desired + avoid;

        // constrain by max accel
        Vector3 dv = final - rb.linearVelocity;
        float maxDelta = maxAccel * Time.fixedDeltaTime;
        if (dv.magnitude > maxDelta) dv = dv.normalized * maxDelta;

        rb.AddForce(dv, ForceMode.VelocityChange);
    }

    Vector3 ComputeAvoidance()
    {
        Vector3 avoid = Vector3.zero;
        Vector3 normalSum = Vector3.zero;
        int rays = Mathf.Max(3, avoidRays);
        float halfFOV = 60f;
        for (int i = 0; i < rays; i++)
        {
            float a = Mathf.Lerp(-halfFOV, halfFOV, (float)i / (rays - 1));
            Quaternion rot = Quaternion.AngleAxis(a, transform.up);
            Vector3 dir = rot * transform.forward;
            if (Physics.SphereCast(transform.position, 0.25f, dir, out RaycastHit hit, avoidDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // accumulate normals for averaged plane/sliding
                normalSum += hit.normal;

                // repulsion away from hit point
                float closeness = Mathf.Clamp01((avoidDistance - hit.distance) / avoidDistance);
                Vector3 repel = (transform.position - hit.point).normalized * (closeness * avoidStrength * 0.6f);
                avoid += repel;

                // compute slide direction by projecting forward on hit plane
                Vector3 slideDir = Vector3.ProjectOnPlane(transform.forward, hit.normal).normalized;
                Vector3 slide = slideDir * (closeness * avoidStrength);
                avoid += slide * 0.6f;
            }
        }

        if (normalSum.sqrMagnitude > 1e-6f)
        {
            Vector3 avgNormal = normalSum.normalized;
            Vector3 desiredDir = (transform.forward).normalized;
            Vector3 slideDesired = Vector3.ProjectOnPlane(desiredDir, avgNormal) * avoidStrength * 0.5f;
            avoid += slideDesired;
        }

        // clamp magnitude to avoid excessive corrections
        float maxAvoid = avoidStrength * 1.8f;
        if (avoid.magnitude > maxAvoid) avoid = avoid.normalized * maxAvoid;

        if (drawDebugGizmos)
        {
            Debug.DrawRay(transform.position, avoid, Color.yellow);
        }

        return avoid;
    }

    // Stuck detection and unstuck logic
    void CheckStuckAndUnstuck()
    {
        // if slowed down and detected nearby geometry, apply impulse away
        if (rb.linearVelocity.magnitude < stuckVelocityThreshold)
        {
            Collider[] cols = Physics.OverlapSphere(transform.position, stuckProbeRadius, obstacleMask, QueryTriggerInteraction.Ignore);
            if (cols != null && cols.Length > 0)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer > stuckTimeThreshold)
                {
                    Vector3 push = Vector3.zero;
                    foreach (var c in cols)
                    {
                        if (c == null) continue;
                        Vector3 closest = c.ClosestPoint(transform.position);
                        push += (transform.position - closest).normalized;
                    }
                    if (push.sqrMagnitude < 1e-6f) push = (transform.position - player.position).normalized;
                    push.Normalize();
                    rb.AddForce(push * unstuckForce, ForceMode.VelocityChange);
                    stuckTimer = 0f;
                }
                return;
            }
        }

        stuckTimer = 0f;
    }

    void RotateToward(Vector3 worldTarget)
    {
        Vector3 toTarget = worldTarget - transform.position;
        if (toTarget.sqrMagnitude < 0.01f) return;
        Quaternion want = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        Quaternion cur = rb.rotation;
        Quaternion nxt = Quaternion.Slerp(cur, want, Mathf.Clamp01(rotationSpeed * Time.fixedDeltaTime));
        rb.MoveRotation(nxt);
    }

    void AlignToVelocity()
    {
        Vector3 vel = rb.linearVelocity;
        if (vel.sqrMagnitude < 0.5f) return;
        Quaternion want = Quaternion.LookRotation(vel.normalized, Vector3.up);
        Quaternion nxt = Quaternion.Slerp(rb.rotation, want, Mathf.Clamp01(rotationSpeed * 0.6f * Time.fixedDeltaTime));
        rb.MoveRotation(nxt);
    }

    Vector3 ComputeEvade()
    {
        float t = Time.time * evadeFrequency + hoverSeed;
        return transform.right * Mathf.Sin(t) * evadeAmplitude + transform.up * Mathf.Cos(t * 0.7f) * (evadeAmplitude * 0.6f);
    }

    bool CanSeePlayer()
    {
        if (player == null) return false;
        Vector3 toPlayer = player.position - transform.position;
        float d = toPlayer.magnitude;
        if (d > detectionRange) return false;
        float ang = Vector3.Angle(transform.forward, toPlayer);
        if (ang > detectionAngle * 0.5f) return false;
        return HasLineOfSightTo(player.position);
    }

    bool HasLineOfSightTo(Vector3 worldPos)
    {
        Vector3 origin = firingPosition != null ? firingPosition.position : transform.position;
        Vector3 dir = worldPos - origin;
        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (player != null && (hit.collider.transform.IsChildOf(player) || hit.collider.transform == player)) return true;
            return false;
        }
        return true;
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

    void FireAtPlayer()
    {
        if (projectilePrefab == null || player == null) return;

        // choose muzzles: prefer firingPositions array if populated, otherwise fallback to single firingPosition
        Transform[] muzzles = firingPositions != null && firingPositions.Length > 0 ? firingPositions : (firingPosition != null ? new Transform[] { firingPosition } : null);
        if (muzzles == null || muzzles.Length == 0) return;

        if (sequentialMuzzleFire)
        {
            StartCoroutine(FireMuzzlesSequential(muzzles));
        }
        else
        {
            // fire all at once
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
        Vector3 shooterVel = rb != null ? rb.linearVelocity : Vector3.zero;

        // Base forward direction from muzzle
        Vector3 aimDir = muzzle.forward;

        // Compute a lead direction based on target motion and projectile speed
        Vector3 targetVel = playerRb != null ? playerRb.linearVelocity : Vector3.zero;
        Vector3 leadDir = ComputeLeadDirection(shooterPos, player.position, shooterVel, targetVel, projectileSpeed);

        // Blend between straight-fire and computed lead according to aimPredictionFactor
        if (aimPredictionFactor > 0f)
        {
            aimDir = Vector3.Slerp(aimDir, leadDir, Mathf.Clamp01(aimPredictionFactor)).normalized;
        }

        // Apply inaccuracy/spread on top
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

    void AlarmNearby()
    {
        if (!alarmOthers) return;

        Collider[] cols = Physics.OverlapSphere(transform.position, alarmRange);
        foreach (var c in cols)
        {
            if (c == null) continue;
            var other = c.GetComponent<NewBasicEnemyAI>();
            if (other == null || other == this) continue;
            other.lastKnownPlayerPos = this.lastKnownPlayerPos;
            other.lastSeenTime = Time.time;
            if (other.state != State.Chasing)
                other.state = State.Chasing;
        }
    }

    IEnumerator SearchThenReturn()
    {
        state = State.Searching;
        float end = Time.time + loseSightTime;
        while (Time.time < end)
        {
            // hover in place, if player appears resume chase
            rb.AddForce(ComputeEvade() * 0.3f, ForceMode.Acceleration);
            yield return new WaitForSeconds(0.18f);
            if (CanSeePlayer())
            {
                state = State.Chasing;
                yield break;
            }
        }

        // go back to nearest patrol point // dunno if working
        state = State.Returning;
        int nearest = 0;
        float best = float.MaxValue;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float d = Vector3.Distance(transform.position, patrolPoints[i]);
            if (d < best) { best = d; nearest = i; }
        }
        patrolIndex = nearest;
    }

    #region Damage/Death
    public void TakeDamage(int amount)
    {
        // Move towards the player when hit
        if (player != null)
            lastKnownPlayerPos = player.position;
        if (lastKnownPlayerPos != null)
        {
            lastSeenTime = Time.time;
            state = State.Chasing;
        }

        // If we have registered weak spots, body hits do not damage the boss; weak spots control death
        if (weakSpots != null && weakSpots.Length > 0)
        {
            // still alert/aggro but do not subtract health
            return;
        }

        if (requireEyeHits)
        {
            // Body was hit but boss only takes damage via eye; still react but don't subtract health.
            return;
        }

        ApplyDamage(amount);
    }

    // Called by weak spots when destroyed
    public void WeakSpotDestroyed(BossWeakSpot spot)
    {
        if (weakSpots == null) return;
        bool all = true;
        foreach (var ws in weakSpots)
        {
            if (ws != null && !ws.isDestroyed) { all = false; break; }
        }
        if (all) Die();
    }

    // Apply actual damage regardless of requireEyeHits flag (used by BossEye)
    public void ApplyDamage(int amount)
    {
        health -= amount;
        if (health <= 0) Die();
    }

    public void Die()
    {
        Destroy(gameObject);
    }
    #endregion

    void OnDrawGizmosSelected()
    {

        Gizmos.color = Color.cyan;
        if (patrolCenter != null) Gizmos.DrawWireCube(patrolCenter.position, patrolBoxSize);
        else Gizmos.DrawWireCube(transform.position, patrolBoxSize);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Vector3 left = Quaternion.Euler(0, -detectionAngle * 0.5f, 0) * transform.forward * detectionRange;
        Vector3 right = Quaternion.Euler(0, detectionAngle * 0.5f, 0) * transform.forward * detectionRange;
        Gizmos.DrawLine(transform.position, transform.position + left);
        Gizmos.DrawLine(transform.position, transform.position + right);
    }
}