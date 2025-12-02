using UnityEngine;
using static UnityEditor.PlayerSettings;

public class BasicEnemy : MonoBehaviour
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
    float distanceToPlayer;
    float timer;
    bool detectPlayer = false;
    void Start()
    {
        Target = GameObject.FindAnyObjectByType<PlayerController>().transform;
    }

    // Update is called once per frame
    void Update() {


        distanceToPlayer = Vector3.Distance(this.transform.position, Target.position);
        rotation = Quaternion.LookRotation(Target.position - transform.position);
        searchForPlayer();

        if (detectPlayer) {
            var nearbyAI = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (var ai in nearbyAI) {
                if(Vector3.Distance(this.transform.position, ai.transform.position) < alarmRange) {
                    ai.transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * dampening);
                }

            }
                //Rotating around target, kinda broken gonna figure out another way.
                //transform.RotateAround(Target.position, Vector3.up, movementspeed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * dampening);
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
        irb.AddForce(transform.forward*projectileSpeed);


    }
    private void searchForPlayer() {
        var delta = Target.position - transform.position;
        float angle = Vector3.Angle(transform.forward, delta);
        if (distanceToPlayer < detectionRange && angle < detectionAngle) {
            detectPlayer = true;
        }else detectPlayer = false;
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
}
