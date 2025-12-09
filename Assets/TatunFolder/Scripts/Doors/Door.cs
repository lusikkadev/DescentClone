using System.Collections;
using UnityEngine;

public class Door : MonoBehaviour
{
    // Door either slides up or to the side when opened, no rotation

    [Header("Door Settings")]
    [SerializeField] bool isOpen = false;
    [SerializeField] float openAmount = 0f;
    [SerializeField] float openSpeed = 2f;
    [SerializeField] bool slideDown = false;
    [SerializeField] bool slideLeft = false;
    [SerializeField] bool slideRight = false;
    private Vector3 closedPosition;
    private Vector3 openPosition;


    public void Start()
    {
        closedPosition = transform.position;
    }

    public void OpenDoor()
    {
        if (!isOpen)
        {
            isOpen = true;
            AudioFW.Play("DoorOpening");
            if (slideDown)
                openPosition = closedPosition + (-transform.up * openAmount);
            else if (slideLeft)
                openPosition = closedPosition + (-transform.right * openAmount);
            else if (slideRight)
                openPosition = closedPosition + (transform.right * openAmount);
            else // Default slide up
                openPosition = closedPosition + (transform.up * openAmount);
            StopAllCoroutines();
            StartCoroutine(MoveDoor(openPosition));
        }
    }

    public void CloseDoor()
    {
        if (isOpen)
        {
            isOpen = false;
            AudioFW.Play("DoorClosing");
            StopAllCoroutines();
            StartCoroutine(MoveDoor(closedPosition));
        }
    }

    IEnumerator MoveDoor(Vector3 targetPosition)
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * openSpeed);
            yield return null;
        }
        transform.position = targetPosition;
    }
}
