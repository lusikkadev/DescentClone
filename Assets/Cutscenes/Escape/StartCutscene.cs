using UnityEngine;
using UnityEngine.Playables;

public class StartCutscene : MonoBehaviour
{
    public GameObject[] disableTheseOnStart;
    public GameObject[] enableTheseOnStart;
    public KeyCode testCutscene;
    PlayableDirector director;

    private void Awake()
    {
        director = GetComponentInChildren<PlayableDirector>();
    }
   public void StartTimeline()
    {
        foreach (var go in disableTheseOnStart)
        {
            go.SetActive(false);
        }
        foreach (var go in enableTheseOnStart)
        {
            go.SetActive(true);
        }
        director.Play();
    }

    void Update()
    {
        if (Input.GetKeyDown(testCutscene))
        {
            StartTimeline();
        }
    }
}
