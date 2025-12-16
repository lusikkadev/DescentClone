using Unity.VisualScripting;
using UnityEngine;

public class EndingScript : MonoBehaviour
{
    public GameObject startcutscene;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.CompareTag("Player")) {
            startcutscene.GetComponent<StartCutscene>().StartTimeline();
        }
       
    }

}

