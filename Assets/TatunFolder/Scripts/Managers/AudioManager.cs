using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] AudioSource m_Source;

    [Header("Gun Sounds")]
    public AudioClip hitScanShot;
    public AudioClip projectileShot;

    private void Awake()
    {
        m_Source = GetComponent<AudioSource>();
    }
    public void PlayHitScanShot()
    {
        m_Source.PlayOneShot(hitScanShot);
    }

    public void PlayProjectileShot()
    {
        m_Source.PlayOneShot(projectileShot);
    }
}
