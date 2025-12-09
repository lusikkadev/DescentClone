using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class TriggerEvent : MonoBehaviour
{
    // Invoke an event when another collider enters this trigger.
    // For doors need to check player stat manager for key possession.
    public UnityEvent onDoorTriggerEnter;
    public UnityEvent onDoorTriggerExit;

    [Header("Event Settings")]
    [SerializeField] bool needRedKey = false;
    [SerializeField] bool needBlueKey = false;
    [SerializeField] bool needGreenKey = false;
    [SerializeField] bool hasRequiredKey = false;

    private void Start()
    {
        if (onDoorTriggerEnter == null)
            onDoorTriggerEnter = new UnityEvent();
        if (onDoorTriggerExit == null)
            onDoorTriggerExit = new UnityEvent();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (!needRedKey && !needBlueKey && !needGreenKey)
        {
            onDoorTriggerEnter.Invoke();
            return;
        }

        if (StatManager.Instance == null)
            return;

        // KeyChecks

        hasRequiredKey = false;
        if (needRedKey && StatManager.Instance.hasRedKey)
            hasRequiredKey = true;
        if (needBlueKey && StatManager.Instance.hasBlueKey)
            hasRequiredKey = true;
        if (needGreenKey && StatManager.Instance.hasGreenKey)
            hasRequiredKey = true;

        if (hasRequiredKey)
        {
            onDoorTriggerEnter.Invoke();
        }
        else
        {
            Debug.Log("Player does not have the required key");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        onDoorTriggerExit.Invoke();
    }
}
