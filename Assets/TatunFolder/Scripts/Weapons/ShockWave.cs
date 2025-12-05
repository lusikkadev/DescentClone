using UnityEngine;

public class ShockWave : MonoBehaviour
{
    float force = 500f;
    float lifeTime = 0.5f;


    private void Update()
    {
        if (enabled)
        {
            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy"))
        {
            // add forece to enemy rigidbody

            Rigidbody enemyRigidbody = other.gameObject.GetComponent<Rigidbody>();
            {
                Vector3 direction = other.transform.position - transform.position;
                direction.y = 0;
                direction.Normalize();
                float forceMagnitude = force;
                enemyRigidbody.AddForce(direction * forceMagnitude);
            }
        }
    }
}
