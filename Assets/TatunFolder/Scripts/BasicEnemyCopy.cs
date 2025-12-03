using UnityEngine;

public class BasicEnemyCopy : MonoBehaviour, IDamageable
{
    Transform Target;
    public float dampening;
    public Transform firingPosition;
    public GameObject Projectile;
    public float movementspeed = 10f;
    public float firingInterval;
    public float projectileSpeed;
    public int health = 50;
    float detectionRange = 200f;
    float detectionAngle = 360f;
    public float alarmRange = 30;
    Quaternion rotation;
    float distanceToPlayer;
    float timer;
    bool detectPlayer = false;

    // New movement / avoidance tuning
    [Header("Engagement")]
    public float desiredEngageDistance = 15f;   // preferred distance to player
    public float orbitSpeed = 1f;               // speed of orbiting motion
    [Header("Wall Avoidance")]
    public float wallAvoidanceDistance = 6f;    // how far ahead we check for obstacles
    public float sphereCastRadius = 0.5f;       // radius for sphere casts
    public LayerMask obstacleMask = ~0;         // which layers count as obstacles
    public float avoidanceForce = 8f;           // how strongly we steer away from obstacles
    public float orbitWeight = 0.6f;            // how strong the orbiting vector is compared to approach

    void Start()
    {
        Target = GameObject.FindAnyObjectByType<PlayerController>().transform;
        movementspeed = Random.Range(movementspeed - 2, movementspeed + 2);
    }

    // Update is called once per frame
    void Update()
    {
        if (Target == null)
            return;

        distanceToPlayer = Vector3.Distance(this.transform.position, Target.position);
        rotation = Quaternion.LookRotation(Target.position - transform.position);
        searchForPlayer();

        if (detectPlayer)
        {
            var nearbyAI = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var ai in nearbyAI)
            {
                if (Vector3.Distance(this.transform.position, ai.transform.position) < alarmRange)
                {
                    ai.transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * dampening);
                }
            }

            // Keep looking at the player smoothly
            transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * dampening);

            // Movement: approach / maintain distance + orbiting + wall avoidance
            Vector3 moveDir = ComputeEngagementMovement();

            // Apply movement
            if (moveDir.sqrMagnitude > 0.0001f)
            {
                transform.position += moveDir.normalized * movementspeed * Time.deltaTime;
            }

            // Firing
            timer += Time.deltaTime;
            while (timer > firingInterval)
            {
                fireWeapon();
                timer -= firingInterval;
            }
        }
    }

    private Vector3 ComputeEngagementMovement()
    {
        Vector3 toTarget = Target.position - transform.position;
        float dist = toTarget.magnitude;
        Vector3 toDir = toTarget.normalized;

        // 1) Approach / retreat to maintain desired distance
        Vector3 engagement = Vector3.zero;
        float distanceTolerance = 1f;
        if (dist > desiredEngageDistance + distanceTolerance)
            engagement = toDir; // move closer
        else if (dist < desiredEngageDistance - distanceTolerance)
            engagement = -toDir; // back off slightly

        // 2) Orbit component (perpendicular to the vector to the player)
        // Use world up as the primary axis for predictable orbiting; for full 6DOF you could choose another axis
        Vector3 orbitAxis = Vector3.up;
        // If toDir is nearly parallel to orbitAxis, fallback to transform.right to avoid zero cross
        if (Mathf.Abs(Vector3.Dot(toDir, orbitAxis)) > 0.99f)
            orbitAxis = transform.right;
        Vector3 orbitDir = Vector3.Cross(toDir, orbitAxis).normalized;
        // Make orbit direction vary a bit so enemies don't all orbit identically
        float sign = Mathf.Sign(Mathf.Sin(Time.time * orbitSpeed + transform.GetInstanceID()));
        orbitDir *= sign;

        Vector3 combined = engagement + orbitDir * orbitWeight;

        // 3) Wall / environment avoidance using several spherecasts around the forward hemisphere
        Vector3 avoidance = Vector3.zero;
        Vector3 origin = transform.position;
        Vector3[] sampleDirs = new Vector3[]
        {
            transform.forward,
            (transform.forward + transform.right).normalized,
            (transform.forward - transform.right).normalized,
            transform.right,
            -transform.right,
            transform.up,
            -transform.up
        };

        for (int i = 0; i < sampleDirs.Length; i++)
        {
            Vector3 dir = sampleDirs[i];
            if (Physics.SphereCast(origin, sphereCastRadius, dir, out RaycastHit hit, wallAvoidanceDistance, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                // steer away from hit by using hit.normal and weighting by penetration distance
                float pushStrength = (wallAvoidanceDistance - hit.distance) / wallAvoidanceDistance;
                avoidance += hit.normal * pushStrength;
            }
        }

        if (avoidance.sqrMagnitude > 0.0001f)
        {
            combined += avoidance.normalized * avoidanceForce;
        }

        return combined;
    }

    private void fireWeapon()
    {
        GameObject projectileInstance = Instantiate(Projectile, firingPosition.position, transform.rotation);
        var irb = projectileInstance.GetComponent<Rigidbody>();
        if (irb != null)
            irb.AddForce(transform.forward * projectileSpeed);
    }

    private void searchForPlayer()
    {
        var delta = Target.position - transform.position;
        float angle = Vector3.Angle(transform.forward, delta);
        if (distanceToPlayer < detectionRange && angle < detectionAngle)
        {
            detectPlayer = true;
        }
        else detectPlayer = false;
    }

    private void OnDrawGizmos()
    {
        var clockwise = Quaternion.Euler(0, -detectionAngle, 0);
        var counterClockwise = Quaternion.Euler(0, detectionAngle, 0);
        var longForward = transform.forward * detectionRange;
        var left = counterClockwise * longForward;
        var right = clockwise * longForward;
        var p = transform.position;
        Debug.DrawLine(p, p + left);
        Debug.DrawLine(p, p + right);

        // Draw desired engage distance and wall avoidance sphere for debugging
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, desiredEngageDistance);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wallAvoidanceDistance);
    }

    public void TakeDamage(float amount)
    {
        health -= (int)amount;
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}
