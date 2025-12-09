using UnityEngine;
public class BasicEnemy : MonoBehaviour, IDamageable
{
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
    Quaternion rotation;
    public float playerPivotRangeTarget;
    Rigidbody rb;
    float distanceToPlayer;
    float timer;


    public EnemyAlarm alarmScript;
    public bool detectPlayer = false;
    void Start()
    {
        Target = GameObject.FindAnyObjectByType<PlayerController>().transform;
        rb = GetComponent<Rigidbody>();

    }

    // Update is called once per frame
    void Update()
    {
        distanceToPlayer = Vector3.Distance(this.transform.position, Target.position);
        rotation = Quaternion.LookRotation(Target.position - transform.position);
        searchForPlayer();

        if (alarmScript.alarmed) {
            detectPlayer = true;
        }

        if (detectPlayer)
        {
            //Wake up nearby AI
            var nearbyAI = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var ai in nearbyAI)
            {
                if (Vector3.Distance(this.transform.position, ai.transform.position) < alarmRange)
                {
                    ai.GetComponent<EnemyAlarm>().alarmed = true;
                }

            }
            if (distanceToPlayer > playerPivotRangeTarget)
            {
                var forwardVector = rotation * Vector3.forward;
                rb.AddForce(forwardVector * movementspeed, ForceMode.Impulse);
            }
            if (distanceToPlayer < playerPivotRangeTarget)
            {
                var backwardsVector = rotation * Vector3.back;
                rb.AddForce(backwardsVector * movementspeed, ForceMode.Impulse);
            }

            this.transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * dampening);
            timer += Time.deltaTime;
            while (timer > firingInterval)
            {
                fireWeapon();
                timer -= firingInterval;
            }
        }

    }
    private void fireWeapon()
    {

        GameObject projectileInstance = Instantiate(Projectile, firingPosition.position, transform.rotation);
        var irb = projectileInstance.GetComponent<Rigidbody>();
        irb.AddForce(transform.forward * projectileSpeed);


    }
    private void searchForPlayer()
    {
        var delta = Target.position - transform.position;
        float angle = Vector3.Angle(transform.forward, delta);
        if (distanceToPlayer < detectionRange && angle < detectionAngle)
        {
            detectPlayer = true;
            alarmScript.alarmed = true;
        }
        else detectPlayer = false;
        alarmScript.alarmed = false;
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
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        // kaikki anims ja efektit tänne
        Destroy(gameObject);
    }

}
