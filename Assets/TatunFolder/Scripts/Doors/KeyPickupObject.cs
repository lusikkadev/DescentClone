using UnityEngine;

public class KeyPickupObject : MonoBehaviour
{
    // Key pickup object script. Attach to keys and set the key Color in inspector.
    [SerializeField] bool isRedKey = false;
    [SerializeField] bool isBlueKey = false;
    [SerializeField] bool isYellowKey = false;
    [SerializeField] bool isBlackKey = false;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (isRedKey)
            {
                StatManager.Instance.hasRedKey = true;
            }
            if (isBlueKey)
            {
                StatManager.Instance.hasBlueKey = true;
            }
            if (isYellowKey)
            {
                StatManager.Instance.hasYellowKey = true;
            }
            if (isBlackKey)
            {
                StatManager.Instance.hasBlackKey = true;
            }
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        
        transform.Rotate(Vector3.up * 50f * Time.deltaTime);
        transform.Rotate(Vector3.right * 30f * Time.deltaTime);
    }
}

