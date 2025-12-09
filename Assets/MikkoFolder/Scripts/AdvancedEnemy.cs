using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class AdvancedEnemy : MonoBehaviour, IDamageable {
    Transform Target;
    public float dampening;
    public Transform firingPosition;
    public GameObject Projectile;
    public float movementspeed;
    public float firingInterval;
    public float projectileSpeed;
    public int health = 50;
    public float detectionRange = 120;
    public float detectionAngle = 45f;
    public float alarmRange = 30;

    Rigidbody rb;
    float distanceToPlayer;
    Quaternion rotation;
    float timer;
    public Transform WaypointL;
    public Transform WaypointR;
    float distanceToWaypointL;
    float distanceToWaypointR;

    public EnemyAlarm alarmScript;
    public bool detectPlayer = false;
    void Start() {
        Target = GameObject.FindAnyObjectByType<PlayerController>().transform;
        rb = GetComponent<Rigidbody>();


    }

    // Update is called once per frame
    void Update() {
        distanceToPlayer = Vector3.Distance(this.transform.position, Target.position);
        distanceToWaypointL = Vector3.Distance(this.transform.position, WaypointL.position);
        distanceToWaypointR = Vector3.Distance(this.transform.position, WaypointR.position);
        rotation = Quaternion.LookRotation(Target.position - this.transform.position);
        searchForPlayer();


        if (alarmScript.alarmed) {
            detectPlayer = true;
        }

        if (detectPlayer) {
            MoveToWaypointWhileFacingPlayer();


            var nearbyAI = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var ai in nearbyAI) {
                if (Vector3.Distance(this.transform.position, ai.transform.position) < alarmRange) {
                    ai.GetComponent<EnemyAlarm>().alarmed = true;
                }

            }

            this.transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * dampening);
            timer += Time.deltaTime;
            while (timer > firingInterval) {
                fireWeapon();
                timer -= firingInterval;
            }
        }

    }
    private void fireWeapon() {

        GameObject projectileInstance = Instantiate(Projectile, firingPosition.position, transform.rotation);
        var irb = projectileInstance.GetComponent<Rigidbody>();
        irb.AddForce(transform.forward * projectileSpeed);


    }
    private void searchForPlayer() {
        var delta = Target.position - transform.position;
        float angle = Vector3.Angle(transform.forward, delta);
        if (distanceToPlayer < detectionRange && angle < detectionAngle) {
            detectPlayer = true;
        }
        else detectPlayer = false;
    }

    private void OnDrawGizmos() {
        var clockwise = Quaternion.Euler(0, -detectionAngle, 0);
        var counterClockwise = Quaternion.Euler(0, detectionAngle, 0);
        var longForward = transform.forward * detectionRange;
        var left = counterClockwise * longForward;
        var right = clockwise * longForward;
        var p = transform.position;
        Debug.DrawLine(p, p + left);
        Debug.DrawLine(p, p + right);
    }

    public void TakeDamage(int amount) {
        health -= amount;
        if (health <= 0) {
            Die();
        }
    }

    public void Die() {
        // kaikki anims ja efektit tänne
        Destroy(gameObject);
    }

void MoveToWaypointWhileFacingPlayer() {
    Transform waypoint =
        (distanceToWaypointL > distanceToWaypointR ? WaypointR : WaypointL);

    Vector3 moveDir = (waypoint.position - transform.position).normalized;
    rb.AddForce(moveDir * movementspeed, ForceMode.Force);

    Vector3 lookDir = (Target.position - transform.position).normalized;

    Quaternion lookRot = Quaternion.LookRotation(lookDir);
    transform.rotation = Quaternion.Slerp(
                                    transform.rotation,
                                    lookRot,
                                    Time.deltaTime * dampening
    );
}
}