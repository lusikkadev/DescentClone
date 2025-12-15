using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class EnemyMover : MonoBehaviour
{
    public enum State { Patrol, Chasing, Searching, Returning }

    [Header("Patrol (box)")]
    public Transform patrolCenter; // default self position
    public Vector3 patrolBoxSize = new Vector3(10f, 4f, 6f);
    public int patrolPointCount = 4;
    public float patrolSpeed = 6f;
    public float patrolReach = 1.0f;
    public bool patrolRandomize = true;

    [Header("Chase / Movement")]
    public float detectionRange = 40f;
    [Range(0f, 180f)] public float detectionAngle = 90f;
    public LayerMask obstacleMask = ~0;
    public float chaseSpeed = 16f;
    public float desiredCombatDistance = 10f;
    public float loseSightTime = 3.0f;

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
    public bool enableOrbit = false;
    public float orbitSpeed = 2.0f;
    public float orbitRadius = 0.6f;
    public bool orbitClockwise = false;

    [Header("Rotation")]
    public float rotationSpeed = 60f; // degrees per second
    [Tooltip("If true, boss will only yaw (rotate around up axis) to face the player.")]
    public bool constrainToYaw = true;
    [Tooltip("When constraining to yaw, use world up (true) or local up (false).")]
    public bool useWorldUp = true;

    [Header("Alarm")]
    public float alarmRange = 18f;
    public bool alarmOthers = true;

    [Header("Stuck detection / Unstuck")]
    public float stuckTimeThreshold = 0.6f;
    public float stuckProbeRadius = 0.6f;
    public float stuckVelocityThreshold = 0.6f;
    public float unstuckForce = 6f;

    [Header("Debug")]
    public bool drawDebugGizmos = false;

    [Header("Assembly options")]
    [Tooltip("If true, child turrets with Rigidbodies will be set kinematic at Start so the boss moves as a single body.")]
    public bool enforceChildRigidbodiesKinematic = true;
    [Header("Rewards")]
    [Tooltip("Optional: prefab to spawn when all boss parts (turrets) are destroyed.")]
    public GameObject keyPickupPrefab;

    // internal
    bool keySpawned = false;

    Rigidbody rb;
    Transform player;
    Rigidbody playerRb;
    Vector3[] patrolPoints;
    int patrolIndex = 0;
    State state = State.Patrol;

    Vector3 lastKnownPlayerPos;
    float lastSeenTime = -999f;
    float hoverSeed;

    float stuckTimer = 0f;

    public Transform Player => player;
    public Rigidbody Rb => rb;
    public Vector3 LastKnownPlayerPos => lastKnownPlayerPos;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        player = FindObjectOfType<PlayerController>()?.transform;
        playerRb = player != null ? player.GetComponent<Rigidbody>() : null;

        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.freezeRotation = true;
            rb.useGravity = false;
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

        if (enforceChildRigidbodiesKinematic)
        {
            var turrets = GetComponentsInChildren<EnemyTurret>(true);
            foreach (var t in turrets)
            {
                if (t == null) continue;
                var tRb = t.GetComponent<Rigidbody>();
                if (tRb != null)
                {
                    tRb.isKinematic = true;
                    tRb.detectCollisions = true;
                }
            }
        }
    }

    void Update()
    {
        // spawn key pickup when all turrets (boss parts) are destroyed
        if (!keySpawned)
            CheckAndSpawnKeyIfAllTurretsDestroyed();

        if (player == null) return;

        if (CanSeePlayer())
        {
            lastKnownPlayerPos = player.position;
            lastSeenTime = Time.time;
            if (state != State.Chasing) BecomeAware();

            // ensure we enter chasing state when player is seen
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

    // Allow turrets to alert the mover when they spot the player
    public void AlertAt(Vector3 worldPos)
    {
        lastKnownPlayerPos = worldPos;
        lastSeenTime = Time.time;
        state = State.Chasing;
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        rb.angularVelocity = Vector3.zero;

        rb.linearVelocity *= linearDamping;

        CheckStuckAndUnstuck();

        switch (state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.Chasing: UpdateChase(); break;
            case State.Searching: UpdateSearchHover(); break;
            case State.Returning: UpdateReturn(); break;
        }
    }

    public void BecomeAware()
    {
        lastSeenTime = Time.time;
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

    void UpdatePatrol()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        Vector3 goal = patrolPoints[patrolIndex];
        MoveTowardsGoal(goal, patrolSpeed);

        if (Vector3.Distance(transform.position, goal) < patrolReach)
            patrolIndex = (patrolIndex + 1) % patrolPoints.Length;

        AlignToVelocity();
    }

    void UpdateChase()
    {
        if (player == null) return;

        Vector3 toPlayer = lastKnownPlayerPos - transform.position;
        Vector3 dir = toPlayer.normalized;

        Vector3 evade = (transform.right * Mathf.Sin((Time.time + hoverSeed) * evadeFrequency) +
                         transform.up * Mathf.Cos((Time.time + hoverSeed) * evadeFrequency * 0.7f)) * evadeAmplitude;

        Vector3 orbitOffset = Vector3.zero;
        if (enableOrbit)
        {
            Vector3 worldUp = Vector3.up; // keep upright orbit plane
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

        if (drawDebugGizmos)
        {
            Debug.DrawLine(transform.position, combatPos, Color.magenta);
            Debug.DrawRay(lastKnownPlayerPos, orbitOffset, Color.cyan);
        }
    }

    void UpdateSearchHover()
    {
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

    void MoveTowardsGoal(Vector3 worldGoal, float speed)
    {
        Vector3 toGoal = worldGoal - transform.position;
        if (toGoal.sqrMagnitude < 0.01f) return;

        Vector3 desired = toGoal.normalized * speed;

        Vector3 avoid = ComputeAvoidance();
        Vector3 final = desired + avoid;

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
                normalSum += hit.normal;

                float closeness = Mathf.Clamp01((avoidDistance - hit.distance) / avoidDistance);
                Vector3 repel = (transform.position - hit.point).normalized * (closeness * avoidStrength * 0.6f);
                avoid += repel;

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

        float maxAvoid = avoidStrength * 1.8f;
        if (avoid.magnitude > maxAvoid) avoid = avoid.normalized * maxAvoid;

        if (drawDebugGizmos)
        {
            Debug.DrawRay(transform.position, avoid, Color.yellow);
        }

        return avoid;
    }

    void CheckStuckAndUnstuck()
    {
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
                    if (push.sqrMagnitude < 1e-6f && player != null) push = (transform.position - player.position).normalized;
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

        Vector3 up = useWorldUp ? Vector3.up : transform.up;

        Quaternion want;
        if (constrainToYaw)
        {
            Vector3 forwardProj = Vector3.ProjectOnPlane(toTarget, up);
            if (forwardProj.sqrMagnitude < 1e-6f) return;
            want = Quaternion.LookRotation(forwardProj.normalized, up);
        }
        else
        {
            want = Quaternion.LookRotation(toTarget.normalized, up);
        }

        Quaternion cur = rb.rotation;
        Quaternion nxt = Quaternion.RotateTowards(cur, want, rotationSpeed * Time.fixedDeltaTime);
        rb.MoveRotation(nxt);
    }

    void AlignToVelocity()
    {
        Vector3 vel = rb.linearVelocity;
        if (vel.sqrMagnitude < 0.5f) return;

        Vector3 up = useWorldUp ? Vector3.up : transform.up;
        Vector3 forwardProj = Vector3.ProjectOnPlane(vel, up);
        if (forwardProj.sqrMagnitude < 1e-6f) return;

        Quaternion want = Quaternion.LookRotation(forwardProj.normalized, up);
        Quaternion nxt = Quaternion.RotateTowards(rb.rotation, want, rotationSpeed * 0.6f * Time.fixedDeltaTime);
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
        Vector3 origin = transform.position;
        Vector3 dir = worldPos - origin;
        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (player != null && (hit.collider.transform.IsChildOf(player) || hit.collider.transform == player)) return true;
            return false;
        }
        return true;
    }

    void AlarmNearby()
    {
        if (!alarmOthers) return;

        Collider[] cols = Physics.OverlapSphere(transform.position, alarmRange);
        foreach (var c in cols)
        {
            if (c == null) continue;
            var other = c.GetComponent<EnemyMover>();
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
            rb.AddForce(ComputeEvade() * 0.3f, ForceMode.Acceleration);
            yield return new WaitForSeconds(0.18f);
            if (CanSeePlayer())
            {
                state = State.Chasing;
                yield break;
            }
        }

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

    void CheckAndSpawnKeyIfAllTurretsDestroyed()
    {
        // find any EnemyTurret components in children (including inactive)
        var turrets = GetComponentsInChildren<EnemyTurret>(true);
        bool anyAlive = false;
        foreach (var t in turrets)
        {
            if (t == null) continue;
            // consider turret alive if its GameObject is not null and active in hierarchy
            if (t.gameObject != null && t.gameObject.activeInHierarchy)
            {
                anyAlive = true;
                break;
            }
        }

        if (!anyAlive && !keySpawned && keyPickupPrefab != null)
        {
            Vector3 spawnPos = patrolCenter != null ? patrolCenter.position : transform.position;
            Instantiate(keyPickupPrefab, spawnPos, Quaternion.identity);
            keySpawned = true;
        }
    }
}
