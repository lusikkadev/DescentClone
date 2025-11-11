using UnityEngine;

public class Impact : MonoBehaviour
{
    [SerializeField] float lifeTime = 2f;
    void Awake()
    {
        Destroy(gameObject, lifeTime);
    }

}
