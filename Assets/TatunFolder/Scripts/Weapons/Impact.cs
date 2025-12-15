using UnityEngine;
using UnityEngine.UI;

public class Impact : MonoBehaviour
{
    [SerializeField] float lifeTime = 2f;
    [SerializeField] int damage = 10;
    [SerializeField] LayerMask hitMask;

    void Awake()
    {
        Destroy(gameObject, lifeTime);
        
    }

    private void OnCollisionEnter(Collision collision)
    {

        if (((1 << collision.gameObject.layer) & hitMask) != 0)
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
        //if (collision.gameObject.CompareTag("Player"))
        //{
        //    var statManager = FindAnyObjectByType<StatManager>();
        //    statManager.TakeDamage(damage);
        //    Destroy(gameObject);
        //}
        //else if (!collision.gameObject.CompareTag("Enemy"))
        //{
        //    Destroy(gameObject);
        //}
    }
}
