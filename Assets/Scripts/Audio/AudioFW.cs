using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class AudioFW : MonoBehaviour
{
    // how to use:
    // put sound effects in their own objects under SFX
    // then anywhere in the code, call 'AudioFW.Play(id)'
    // where id is the name of the sound effect object.

    Dictionary<string, AudioSource> sfx = new Dictionary<string, AudioSource>();
    Dictionary<string, AudioSource> loops = new Dictionary<string, AudioSource>();
    Dictionary<string, AudioFWRandomizer> randomSfx = new Dictionary<string, AudioFWRandomizer>();
    public Dictionary<string, List<AudioPosition>> pool = new Dictionary<string, List<AudioPosition>>();

    private GameObject audioPool;

    [SerializeField] private bool showDebugMessages;

    public static void Play(string id)
    {
        instance.PlayImpl(id);
    }

    public static void Play(string id, Transform trans)
    {
        instance.PlayImpl(id, trans);
    }

    public static void Play(string id, Vector3 pos)
    {
        instance.PlayImpl(id, pos);
    }

    public static void PlayLoop(string id)
    {
        instance.PlayLoopImpl(id);
    }

    public static void StopLoop(string id)
    {
        instance.StopLoopImpl(id);
    }

    public static void AdjustPitch(string id, float pitch)
    {
        instance.AdjustPitchImpl(id, pitch);
    }

    void PlayImpl(string id)
    {
        if (!sfx.ContainsKey(id))
        {
            Debug.LogWarning("No sound with ID " + id);
            return;
        }
        var clip = sfx[id].clip;
        if (randomSfx.ContainsKey(id))
        {
            if (showDebugMessages)
                print("randomizing: " + id);

            var clips = randomSfx[id].randomClips;
            if (clips.Length == 0)
            {
                Debug.LogWarning("Randomizer has no clips to pick from, ID: " + id);
                return;
            }
            clip = clips[Random.Range(0, clips.Length)];
        }


        if (!pool.ContainsKey(id))
        {
            if (showDebugMessages)
                Debug.Log("Instantiate first sound");

            AudioPosition audioPosition = InstantiateAudioPosition(id);
            audioPosition.PlaySound2D(clip);
        }
        else
        {
            if (pool[id].Count > 0)
            {
                if (showDebugMessages)
                    Debug.Log("pool has key: " + id + ". List has " + pool[id].Count + " items");

                pool[id].Last().gameObject.SetActive(true);
                pool[id].Last().PlaySound2D(clip);
                pool[id].RemoveAt(pool[id].Count - 1);
            }
            else
            {
                if (showDebugMessages)
                    Debug.Log("Pool empty, instantiate new sound");

                AudioPosition audioPosition = InstantiateAudioPosition(id);
                audioPosition.PlaySound2D(clip);
            }
        }
    }


    public void ReturnAudioPositionToPool(string id, AudioPosition audioPosition)
    {
        if (showDebugMessages)
            Debug.Log(id + " Returned to pool");

        if (!pool.ContainsKey(id))
        {
            if (showDebugMessages)
                Debug.Log("Pool DOES NOT contain key: " + id + ".  Initilize list");

            pool.Add(id, new List<AudioPosition>()); //Here the actual list is created
            pool[id].Add(audioPosition);
        }
        else
        {
            if (showDebugMessages)
                Debug.Log("Pool contains key: " + id);

            pool[id].Add(audioPosition);
        }
    }

    private AudioPosition InstantiateAudioPosition(string id)
    {
        GameObject audioSourceGo = sfx[id].gameObject;
        GameObject newAudioGo = Instantiate(audioSourceGo, audioPool.transform);
        newAudioGo.name = id;
        AudioPosition audioPosition = newAudioGo.AddComponent<AudioPosition>();
        return audioPosition;
    }

    void PlayImpl(string id, Transform transform) //Jani
    {
        if (!sfx.ContainsKey(id))
        {
            Debug.LogWarning("No sound with ID " + id);
            return;
        }

        if (transform == null) // This can happen if audio is hard targeted to specifil transform and that transform has been destroyed.
        {
            Debug.LogWarning("no transform to follow for " + id);
            return;
        }

        var clip = sfx[id].clip;
        if (randomSfx.ContainsKey(id))
        {
            if (showDebugMessages)
                print("randomizing: " + id);

            var clips = randomSfx[id].randomClips;
            if (clips.Length == 0)
            {
                Debug.LogWarning("Randomizer has no clips to pick from, ID: " + id);
                return;
            }
            clip = clips[Random.Range(0, clips.Length)];
        }

        if (!pool.ContainsKey(id))
        {
            if (showDebugMessages)
                Debug.Log("Instantiate first sound");

            AudioPosition audioPosition = InstantiateAudioPosition(id);
            audioPosition.FollowTransform(clip, transform);
        }
        else
        {
            if (pool[id].Count > 0)
            {
                if (showDebugMessages)
                    Debug.Log("pool has key: " + id + ". List has " + pool[id].Count + " items");

                pool[id].Last().gameObject.SetActive(true);
                pool[id].Last().FollowTransform(clip, transform);
                pool[id].RemoveAt(pool[id].Count - 1);
            }
            else
            {
                if (showDebugMessages)
                    Debug.Log("Pool empty, instantiate new sound");

                AudioPosition audioPosition = InstantiateAudioPosition(id);
                audioPosition.FollowTransform(clip, transform);
            }
        }
    }



    void PlayImpl(string id, Vector3 position) //Jani
    {
        if (!sfx.ContainsKey(id))
        {
            Debug.LogWarning("No sound with ID " + id);
            return;
        }

        var clip = sfx[id].clip;
        if (randomSfx.ContainsKey(id))
        {
            if (showDebugMessages)
                print("randomizing: " + id);

            var clips = randomSfx[id].randomClips;
            if (clips.Length == 0)
            {
                Debug.LogWarning("Randomizer has no clips to pick from, ID: " + id);
                return;
            }
            clip = clips[Random.Range(0, clips.Length)];
        }

        if (!pool.ContainsKey(id))
        {
            if (showDebugMessages)
                Debug.Log("Instantiate first sound");

            AudioPosition audioPosition = InstantiateAudioPosition(id);
            audioPosition.PlayAtPosition(clip, position);
        }
        else
        {
            if (pool[id].Count > 0)
            {
                if (showDebugMessages)
                    Debug.Log("pool has key: " + id + ". List has " + pool[id].Count + " items");

                pool[id].Last().gameObject.SetActive(true);
                pool[id].Last().PlayAtPosition(clip, position);
                pool[id].RemoveAt(pool[id].Count - 1);
            }
            else
            {
                if (showDebugMessages)
                    Debug.Log("Pool empty, instantiate new sound");

                AudioPosition audioPosition = InstantiateAudioPosition(id);
                audioPosition.PlayAtPosition(clip, position);
            }
        }
    }

    void PlayLoopImpl(string id)
    {
        if (!loops.ContainsKey(id))
        {
            Debug.LogWarning("No sound with ID " + id);
            return;
        }
        if (!loops[id].isPlaying)
        {
            loops[id].Play();
        }
    }

    void StopLoopImpl(string id)
    {
        if (!loops.ContainsKey(id))
        {
            Debug.LogWarning("No sound with ID " + id);
            return;
        }
        loops[id].Stop();
    }

    void AdjustPitchImpl(string id, float pitch)
    {
        if (!loops.ContainsKey(id))
        {
            Debug.LogWarning("No sound with ID " + id);
            return;
        }
        loops[id].pitch = Mathf.Clamp(pitch, -3f, 3f);

        if (showDebugMessages)
            print("Pitch adjusted");
    }

    static public AudioFW instance
    {
        get
        {
            if (!_instance)
            {
                var a = GameObject.FindObjectsOfType<AudioFW>();
                if (a.Length == 0)
                    Debug.LogWarning("No AudioFW in scene");
                else if (a.Length > 1)
                    Debug.LogWarning("Multiple AudioFW in scene");
                _instance = a[0];
            }
            return _instance;
        }
    }
    static AudioFW _instance;

    void FindAudioSources()
    {
        var audioSources = transform.Find("SFX").GetComponentsInChildren<AudioSource>();
        foreach (var a in audioSources)
        {
            sfx.Add(a.name, a);
            var rand = a.GetComponent<AudioFWRandomizer>();
            if (rand != null)
            {
                randomSfx.Add(a.name, rand);
            }

        }
        var audioSources2 = transform.Find("Loops").GetComponentsInChildren<AudioSource>();
        foreach (var a in audioSources2)
        {
            loops.Add(a.name, a);
        }
    }

    void Awake()
    {
        FindAudioSources();
        audioPool = new GameObject();
        audioPool.name = "AudioPool";
        audioPool.transform.parent = transform;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A) && showDebugMessages)
            DebugPrint();
    }

    void DebugPrint()
    {
        string s = "Audio loaded: ";
        foreach (var id in sfx.Keys)
            s += id + " ";
        print(s);
    }

    #region Old implementation
    ////void PlayImpl(string id)// Original Simple play 2D audio as OneShot
    ////{
    ////    if (!sfx.ContainsKey(id))
    ////    {
    ////        Debug.LogWarning("No sound with ID " + id);
    ////        return;
    ////    }
    ////    var clip = sfx[id].clip;
    ////    if (randomSfx.ContainsKey(id))
    ////    {
    ////        if (showDebugMessages)
    ////            print("randomizing: " + id);

    ////        var clips = randomSfx[id].randomClips;
    ////        if (clips.Length == 0)
    ////        {
    ////            Debug.LogWarning("Randomizer has no clips to pick from, ID: " + id);
    ////            return;
    ////        }
    ////        clip = clips[Random.Range(0, clips.Length)];
    ////    }

    ////    sfx[id].PlayOneShot(clip);
    ////}
    #endregion


}
