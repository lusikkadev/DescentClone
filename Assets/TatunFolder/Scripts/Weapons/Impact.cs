using UnityEngine;

public class Impact : MonoBehaviour
{
    [SerializeField] float lifeTime = 2f;
    [SerializeField] int damage = 10;
    void Awake()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            var statManager = FindAnyObjectByType<StatManager>();
            statManager.TakeDamage(damage);
            Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
