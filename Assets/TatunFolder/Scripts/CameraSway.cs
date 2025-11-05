using UnityEngine;

public class CameraSway : MonoBehaviour
{
    public float swayAmountX = 0.5f;
    public float swayAmountY = 0.5f;
    public float swaySpeed = 2.0f;
    private Vector3 initialPosition;
    [SerializeField] Rigidbody rbPlayer;

    private void Awake()
    {

    }
    void Start()
    {
        initialPosition = transform.localPosition;
    }
    void Update()
    {
        var velocity = rbPlayer.linearVelocity;

        if (velocity == Vector3.zero)
        {
            float swayX = Mathf.Sin(Time.time * swaySpeed) * swayAmountX;
            
            float swayY = Mathf.Cos(Time.time * swaySpeed) * swayAmountY;
            transform.localPosition = initialPosition + new Vector3(swayX, swayY, 0);
        }


    }
}
