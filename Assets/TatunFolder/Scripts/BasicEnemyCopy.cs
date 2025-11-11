using UnityEngine;

public class BasicEnemyCopy : MonoBehaviour, IDamageable
{
    public Transform Target;
    public float dampening;
    public Transform firingPosition;
    public GameObject Projectile;
    public float firingInterval;
    public float projectileSpeed;
    float timer;

    public int health = 50;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        timer += Time.deltaTime;
        var rotation = Quaternion.LookRotation(Target.position - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * dampening);
        if (Input.GetKeyDown(KeyCode.W))
        {
            fireWeapon();
        }
        while (timer > firingInterval)
        {

            fireWeapon();
            timer -= firingInterval;
        }
    }
    private void fireWeapon()
    {

        GameObject projectileInstance = Instantiate(Projectile, firingPosition.position, transform.rotation);
        var irb = projectileInstance.GetComponent<Rigidbody>();
        irb.AddForce(transform.forward * projectileSpeed);

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
