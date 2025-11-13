using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

public class AudioPosition : MonoBehaviour
{

    [Header("Visible for debugging")]
    [SerializeField] private float fadeOutSpeed = 10f;
    [SerializeField] private Transform transformToFollow;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool playAtPosition;
    [SerializeField] private bool volumeFadedOut;
    [SerializeField] private float originalVolume;


    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        originalVolume = audioSource.volume;
    }

    private void OnEnable()
    {
        volumeFadedOut = false;
        audioSource.volume = originalVolume;
    }

    void Update()
    {
        transform.position = transformToFollow ? transformToFollow.position : transform.position;

        if (playAtPosition && !audioSource.isPlaying)
        {
            AudioFW.instance.ReturnAudioPositionToPool(audioSource.name, this);
            gameObject.SetActive(false);
            return;
        }
        else if (transformToFollow == null && !playAtPosition)
        {
            var vol = audioSource.volume - fadeOutSpeed * Time.deltaTime;
            audioSource.volume = Mathf.Clamp01(vol);

            if (audioSource.volume <= 0f && !volumeFadedOut)
            {
                volumeFadedOut = true;
                AudioFW.instance.ReturnAudioPositionToPool(audioSource.name, this);
                gameObject.SetActive(false);
            }
        }
        else if (!audioSource.isPlaying)
        {
            AudioFW.instance.ReturnAudioPositionToPool(audioSource.name, this);
            gameObject.SetActive(false);
        }
    }

    public void PlaySound2D(AudioClip clip)
    {
        playAtPosition = true;
        transformToFollow = null;
        transform.position = Vector3.zero;
        audioSource.clip = clip;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
    }

    public void PlayAtPosition(AudioClip clip, Vector3 pos)
    {
        playAtPosition = true;
        transformToFollow = null;
        transform.position = pos;
        audioSource.spatialBlend = 1f;
        audioSource.clip = clip;
        audioSource.Play();
    }

    public void FollowTransform(AudioClip clip, Transform transform)
    {
        playAtPosition = false;
        transformToFollow = transform;
        audioSource.spatialBlend = 1f;
        audioSource.clip = clip;
        audioSource.Play();
    }

}
